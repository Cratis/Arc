// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace Cratis.Arc.Calendar;

/// <summary>
/// Represents the subject behind <see cref="ClockBoundarySubjectExtensions.ReevaluateAtBoundaries{TSource, TResult}"/>.
/// </summary>
/// <typeparam name="TSource">The type of the candidates.</typeparam>
/// <typeparam name="TResult">The type of the answer.</typeparam>
/// <remarks>
/// Every input (a candidate, a terminal call, a timer fire, a subscription, an unsubscription, a cancellation and disposal)
/// is queued and run by one drainer at a time, on the thread of whichever input found the queue idle. No lock is held while
/// an input runs, so an input raised from inside another (an observer reacting to an emission, a candidate pushed while
/// subscribing, a callback raised while disposing) is queued behind it, never run nested and never waited for. All state
/// below the queue is touched only by the drainer.
/// </remarks>
[SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "The timer and the candidate subscription are disposed by End, which Dispose queues to the drainer that owns them.")]
sealed class ClockBoundarySubject<TSource, TResult> : ISubject<TResult>, IDisposable
{
    /// <summary>
    /// The longest due time a timer accepts.
    /// </summary>
    internal static readonly TimeSpan MaxTimerDueTime = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    readonly ISubject<TSource> _candidates;
    readonly ClockBoundaryContext _context;
    readonly Func<TSource, ClockReading, ClockEvaluation<TResult>> _evaluate;
    readonly Queue<Action> _inputs = new();
    readonly List<Subscription> _subscriptions = [];
    readonly SingleAssignmentDisposable _candidateSubscription = new();
    bool _draining;
    CancellationTokenRegistration _cancellation;
    ITimer? _timer;
    DateTimeOffset? _boundary;
    TSource? _latest;
    TResult? _last;
    Exception? _error;
    bool _hasLast;
    bool _connected;
    bool _ended;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClockBoundarySubject{TSource, TResult}"/> class.
    /// </summary>
    /// <param name="candidates">The candidates.</param>
    /// <param name="context">The <see cref="ClockBoundaryContext"/>.</param>
    /// <param name="evaluate">The evaluation.</param>
    internal ClockBoundarySubject(
        ISubject<TSource> candidates,
        ClockBoundaryContext context,
        Func<TSource, ClockReading, ClockEvaluation<TResult>> evaluate)
    {
        _candidates = candidates;
        _context = context;
        _evaluate = evaluate;
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<TResult> observer)
    {
        var subscription = new Subscription(this, observer);
        Post(() => Join(subscription));
        return subscription;
    }

    /// <inheritdoc/>
    public void OnNext(TResult value) => throw new NotSupportedException("The values of a clock-boundary subject come from its candidates and its clock.");

    /// <inheritdoc/>
    public void OnError(Exception error) => Post(() => End(error, true));

    /// <inheritdoc/>
    public void OnCompleted() => Post(() => End(null, true));

    /// <inheritdoc/>
    public void Dispose() => Post(() => End(null, false));

    static TimeSpan Clamp(TimeSpan due)
    {
        if (due < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return due > MaxTimerDueTime ? MaxTimerDueTime : due;
    }

    void Post(Action input)
    {
        lock (_inputs)
        {
            _inputs.Enqueue(input);
            if (_draining)
            {
                return;
            }

            _draining = true;
        }

        Drain();
    }

    void Drain()
    {
        try
        {
            while (Take() is { } input)
            {
                input();
            }
        }
        catch
        {
            lock (_inputs)
            {
                _draining = false;
            }

            throw;
        }
    }

    Action? Take()
    {
        lock (_inputs)
        {
            _draining = _inputs.TryDequeue(out var input);
            return input;
        }
    }

    void Join(Subscription subscription)
    {
        if (subscription.IsDisposed)
        {
            return;
        }

        if (_ended)
        {
            subscription.End(_error);
            return;
        }

        _subscriptions.Add(subscription);
        if (!_connected)
        {
            Connect();
        }
    }

    void Leave(Subscription subscription)
    {
        if (_subscriptions.Remove(subscription) && _subscriptions.Count == 0)
        {
            End(null, false);
        }
    }

    void Connect()
    {
        _connected = true;
        _cancellation = _context.CancellationToken.Register(() => Post(() => End(null, false)));
        if (_context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            _candidateSubscription.Disposable = _candidates.Subscribe(new CandidateObserver(this));
        }
        catch (Exception ex)
        {
            End(ex, true);
        }
    }

    void Accept(TSource value)
    {
        if (_ended)
        {
            return;
        }

        _latest = value;
        Evaluate(_context.TimeProvider.GetUtcNow());
    }

    void OnClockEpoch()
    {
        if (_ended || _boundary is not DateTimeOffset boundary)
        {
            return;
        }

        var now = _context.TimeProvider.GetUtcNow();
        if (now < boundary)
        {
            ArmTimer(boundary - now);
            return;
        }

        Evaluate(now);
    }

    void Evaluate(DateTimeOffset now)
    {
        var evaluation = TryEvaluate(now);
        if (evaluation is null)
        {
            return;
        }

        Emit(evaluation.Result);
        Schedule(evaluation.NextBoundary, now);
    }

    ClockEvaluation<TResult>? TryEvaluate(DateTimeOffset now)
    {
        try
        {
            using (_context.TenantScope.Begin(_context.Tenant))
            {
                return _evaluate(_latest!, new ClockReading(now, _context.Zone));
            }
        }
        catch (Exception ex)
        {
            End(ex, true);
            return null;
        }
    }

    void Emit(TResult result)
    {
        if (_hasLast && _context.ChangeSetComputor.AreEquivalentResults(_last, result))
        {
            return;
        }

        _last = result;
        _hasLast = true;
        foreach (var subscription in _subscriptions)
        {
            subscription.Next(result);
        }
    }

    /// <summary>
    /// Arms the timer for the boundary of an evaluation at <paramref name="evaluatedAt"/>. The clock is read again because
    /// evaluating and emitting take time: a boundary that passed meanwhile is caught up at once instead of by a timer.
    /// </summary>
    /// <param name="boundary">The next boundary the evaluation stated, or <see langword="null"/> when there is none.</param>
    /// <param name="evaluatedAt">The instant the evaluation ran at.</param>
    void Schedule(DateTimeOffset? boundary, DateTimeOffset evaluatedAt)
    {
        _boundary = boundary;
        if (boundary is not DateTimeOffset next)
        {
            _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            return;
        }

        var now = _context.TimeProvider.GetUtcNow();
        if (next > evaluatedAt && next <= now)
        {
            _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            Post(OnClockEpoch);
            return;
        }

        ArmTimer(next - now);
    }

    void ArmTimer(TimeSpan due)
    {
        due = Clamp(due);
        if (_timer is null)
        {
            _timer = _context.TimeProvider.CreateTimer(_ => Post(OnClockEpoch), null, due, Timeout.InfiniteTimeSpan);
            return;
        }

        _timer.Change(due, Timeout.InfiniteTimeSpan);
    }

    void End(Exception? error, bool forward)
    {
        if (_ended)
        {
            return;
        }

        _ended = true;
        _boundary = null;
        _error = forward ? error : null;
        Subscription[] subscriptions = forward ? [.. _subscriptions] : [];
        _subscriptions.Clear();
        _timer?.Dispose();
        _timer = null;
        _candidateSubscription.Dispose();
        _cancellation.Dispose();
        foreach (var subscription in subscriptions)
        {
            subscription.End(error);
        }
    }

    sealed class Subscription(ClockBoundarySubject<TSource, TResult> owner, IObserver<TResult> observer) : IDisposable
    {
        int _disposed;

        public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

        public void Next(TResult value)
        {
            if (!IsDisposed)
            {
                observer.OnNext(value);
            }
        }

        public void End(Exception? error)
        {
            if (IsDisposed)
            {
                return;
            }

            if (error is null)
            {
                observer.OnCompleted();
                return;
            }

            observer.OnError(error);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                owner.Post(() => owner.Leave(this));
            }
        }
    }

    sealed class CandidateObserver(ClockBoundarySubject<TSource, TResult> owner) : IObserver<TSource>
    {
        public void OnNext(TSource value) => owner.Post(() => owner.Accept(value));

        public void OnError(Exception error) => owner.Post(() => owner.End(error, true));

        public void OnCompleted() => owner.Post(() => owner.End(null, true));
    }
}
