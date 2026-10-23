// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace Cratis.Arc.Calendar;

/// <summary>
/// Represents the subject behind <see cref="ClockBoundarySubjectExtensions.ReevaluateAtBoundaries{TSource, TResult}"/>.
/// </summary>
/// <typeparam name="TSource">The type of the candidates.</typeparam>
/// <typeparam name="TResult">The type of the answer.</typeparam>
sealed class ClockBoundarySubject<TSource, TResult> : ISubject<TResult>, IDisposable
{
    /// <summary>
    /// The longest due time a timer accepts.
    /// </summary>
    internal static readonly TimeSpan MaxTimerDueTime = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    readonly ISubject<TSource> _candidates;
    readonly ClockBoundaryContext _context;
    readonly Func<TSource, ClockReading, ClockEvaluation<TResult>> _evaluate;
    readonly Subject<TResult> _output = new();
    readonly object _gate = new();
    IDisposable? _candidateSubscription;
    CancellationTokenRegistration _cancellation;
    CancellationTokenRegistration _endedCancellation;
    ITimer? _timer;
    DateTimeOffset _boundary;
    TSource? _latest;
    TResult? _last;
    bool _hasLatest;
    bool _hasLast;
    bool _connected;
    bool _ended;
    bool _terminated;
    int _subscribers;

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
        try
        {
            lock (_gate)
            {
                return SubscribeLocked(observer);
            }
        }
        finally
        {
            ReleaseCancellation();
        }
    }

    /// <inheritdoc/>
    public void OnNext(TResult value) => throw new NotSupportedException("The values of a clock-boundary subject come from its candidates and its clock.");

    /// <inheritdoc/>
    public void OnError(Exception error) => End(error, true);

    /// <inheritdoc/>
    public void OnCompleted() => End(null, true);

    /// <inheritdoc/>
    public void Dispose()
    {
        End(null, false);
        lock (_gate)
        {
            _output.Dispose();
        }
    }

    IDisposable SubscribeLocked(IObserver<TResult> observer)
    {
        if (_ended)
        {
            if (_terminated)
            {
                return _output.Subscribe(observer);
            }

            observer.OnCompleted();
            return Disposable.Empty;
        }

        var subscription = _output.Subscribe(observer);
        _subscribers++;
        if (!_connected)
        {
            _connected = true;
            _cancellation = _context.CancellationToken.Register(() => End(null, false));
            if (_ended)
            {
                subscription.Dispose();
                return Disposable.Empty;
            }

            _candidateSubscription = _candidates.Subscribe(new CandidateObserver(this));
        }

        return Disposable.Create(() => Unsubscribe(subscription));
    }

    void OnCandidate(TSource value)
    {
        try
        {
            lock (_gate)
            {
                if (_ended)
                {
                    return;
                }

                _latest = value;
                _hasLatest = true;
                Evaluate();
            }
        }
        finally
        {
            ReleaseCancellation();
        }
    }

    void Unsubscribe(IDisposable subscription)
    {
        var last = false;
        lock (_gate)
        {
            subscription.Dispose();
            if (_subscribers > 0 && --_subscribers == 0)
            {
                last = true;
            }
        }

        if (last)
        {
            End(null, false);
        }
    }

    void End(Exception? error, bool forward)
    {
        lock (_gate)
        {
            EndLocked(error, forward);
        }

        ReleaseCancellation();
    }

    void EndLocked(Exception? error, bool forward)
    {
        if (_ended)
        {
            return;
        }

        _ended = true;
        _timer?.Dispose();
        _timer = null;
        _candidateSubscription?.Dispose();
        _candidateSubscription = null;
        _endedCancellation = _cancellation;
        _cancellation = default;
        if (!forward)
        {
            return;
        }

        _terminated = true;
        if (error is null)
        {
            _output.OnCompleted();
        }
        else
        {
            _output.OnError(error);
        }
    }

    /// <summary>
    /// Disposes the cancellation registration of an ended subject. Disposing a registration waits for a running callback,
    /// which takes the gate, so it is done outside the gate.
    /// </summary>
    void ReleaseCancellation()
    {
        CancellationTokenRegistration registration;
        lock (_gate)
        {
            registration = _endedCancellation;
            _endedCancellation = default;
        }

        registration.Dispose();
    }

    void Evaluate()
    {
        var now = _context.TimeProvider.GetUtcNow();
        ClockEvaluation<TResult> evaluation;
        try
        {
            using (_context.TenantScope.Begin(_context.Tenant))
            {
                evaluation = _evaluate(_latest!, new ClockReading(now, _context.Zone));
            }
        }
        catch (Exception ex)
        {
            EndLocked(ex, true);
            return;
        }

        if (!_hasLast || !_context.ChangeSetComputor.AreEquivalentResults(_last, evaluation.Result))
        {
            _last = evaluation.Result;
            _hasLast = true;
            _output.OnNext(evaluation.Result);
        }

        if (!_ended)
        {
            Arm(evaluation.NextBoundary, now);
        }
    }

    void Arm(DateTimeOffset? boundary, DateTimeOffset now)
    {
        if (boundary is null)
        {
            _timer?.Dispose();
            _timer = null;
            return;
        }

        _boundary = boundary.Value;
        var due = _boundary - now;
        if (due < TimeSpan.Zero)
        {
            due = TimeSpan.Zero;
        }
        else if (due > MaxTimerDueTime)
        {
            due = MaxTimerDueTime;
        }

        if (_timer is null)
        {
            _timer = _context.TimeProvider.CreateTimer(OnTimer, null, due, Timeout.InfiniteTimeSpan);
        }
        else
        {
            _timer.Change(due, Timeout.InfiniteTimeSpan);
        }
    }

    void OnTimer(object? state)
    {
        try
        {
            lock (_gate)
            {
                if (_ended || !_hasLatest)
                {
                    return;
                }

                var now = _context.TimeProvider.GetUtcNow();
                if (now < _boundary)
                {
                    Arm(_boundary, now);
                    return;
                }

                Evaluate();
            }
        }
        finally
        {
            ReleaseCancellation();
        }
    }

    sealed class CandidateObserver(ClockBoundarySubject<TSource, TResult> owner) : IObserver<TSource>
    {
        public void OnNext(TSource value) => owner.OnCandidate(value);

        public void OnError(Exception error) => owner.End(error, true);

        public void OnCompleted() => owner.End(null, true);
    }
}
