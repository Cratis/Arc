// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Security.Claims;
using System.Threading.Channels;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Queries;

/// <summary>
/// Owns startup, buffered emissions and the shared disposal operation for a query observation.
/// </summary>
/// <param name="admitted">The admitted query and its owned execution scope.</param>
/// <param name="materialize">Materializes collections before their producing scope can change.</param>
/// <param name="ownsScope">Whether this observation owns the admitted execution scope.</param>
internal sealed class ObservedQueryEmissions(QueryResult admitted, Func<object?, object?> materialize, bool ownsScope = true) : IDisposable, IAsyncDisposable
{
    readonly object _lifecycleGate = new();
    readonly Channel<Emission> _emissions = Channel.CreateUnbounded<Emission>();
    readonly CancellationTokenSource _lifetime = new();
    readonly CancellationTokenSource _enumeration = new();
    readonly SemaphoreSlim _readGate = new(1, 1);
    readonly TaskCompletionSource _startup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _callbacksDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _readsDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly IServiceScope? _scope = ownsScope ? admitted.OwnedScope : null;
    Lifecycle _state = Lifecycle.Starting;
    TaskCompletionSource? _disposal;
    IDisposable? _subscription;
    Task? _producer;
    Task<QueryResult?>? _pending;
    QueryResult? _terminal;
    bool _delivered;
    bool _callbacksStopped;
    int _callbacks;
    int _reads;

    enum Lifecycle
    {
        Starting = 0,
        Running = 1,
        Disposing = 2,
        Disposed = 3
    }

    QueryContext Context => admitted.AuthorizedQueryContext!;
    IServiceProvider Services => _scope?.ServiceProvider ?? Context.ServiceProvider!;

    /// <inheritdoc/>
    public void Dispose() => BeginDisposal().GetAwaiter().GetResult();

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => new(BeginDisposal());

    /// <summary>
    /// Creates a live observation with no producer or execution scope.
    /// </summary>
    /// <param name="initial">The snapshot or pipeline rejection.</param>
    /// <returns>The idle emission owner.</returns>
    internal static ObservedQueryEmissions Idle(QueryResult initial)
    {
        var owner = new ObservedQueryEmissions(initial, data => data, ownsScope: false);
        owner.FinishStartup();

        return owner;
    }

    /// <summary>
    /// Starts the admitted producer, settling startup even if subscription fails or disposal has begun.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    internal void Start<TReadModel>()
    {
        try
        {
            lock (_lifecycleGate)
            {
                if (_state != Lifecycle.Starting)
                {
                    return;
                }
            }

            switch (admitted.Data)
            {
                case IObservable<TReadModel> single:
                    Subscribe(single);
                    break;
                case IObservable<IEnumerable<TReadModel>> collection:
                    Subscribe(collection);
                    break;
                case IAsyncEnumerable<TReadModel> enumerable:
                    _producer = Produce(enumerable);
                    break;
            }
        }
        finally
        {
            FinishStartup();
        }
    }

    /// <summary>
    /// Reads and intercepts the next non-suppressed emission without transferring processing ownership to the reader.
    /// </summary>
    /// <param name="timeout">The read deadline.</param>
    /// <param name="cancellationToken">Cancellation for this read.</param>
    /// <returns>The emission or a terminal query result.</returns>
    /// <exception cref="TimeoutException">No emission arrives before the deadline.</exception>
    internal async Task<QueryResult> Read(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var deadline = EnterRead(cancellationToken);
        var entered = false;
        try
        {
            deadline.CancelAfter(timeout);
            await _startup.Task.WaitAsync(deadline.Token).ConfigureAwait(false);
            await _readGate.WaitAsync(deadline.Token).ConfigureAwait(false);
            entered = true;
            if (_terminal is not null)
            {
                return _terminal;
            }

            while (true)
            {
                _pending ??= Process(await _emissions.Reader.ReadAsync(deadline.Token).ConfigureAwait(false));
                var result = await _pending.WaitAsync(deadline.Token).ConfigureAwait(false);
                _pending = null;
                if (result is null)
                {
                    continue;
                }

                _delivered = true;
                return result;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && !_lifetime.IsCancellationRequested)
        {
            throw new TimeoutException($"Timed out waiting {timeout.TotalSeconds:0.###} seconds for the next query emission.");
        }
        finally
        {
            if (entered)
            {
                _readGate.Release();
            }
            ExitRead();
        }
    }

    CancellationTokenSource EnterRead(CancellationToken cancellationToken)
    {
        lock (_lifecycleGate)
        {
            ObjectDisposedException.ThrowIf(_state is Lifecycle.Disposing or Lifecycle.Disposed, this);
            var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
            _reads++;

            return deadline;
        }
    }

    void ExitRead()
    {
        lock (_lifecycleGate)
        {
            _reads--;
            if (_state == Lifecycle.Disposing && _reads == 0)
            {
                _readsDrained.TrySetResult();
            }
        }
    }

    void FinishStartup()
    {
        lock (_lifecycleGate)
        {
            if (_state == Lifecycle.Starting)
            {
                _state = Lifecycle.Running;
            }
        }
        _startup.TrySetResult();
    }

    void Subscribe<T>(IObservable<T> observable)
    {
        var subscription = observable.Subscribe(Observer.Create<T>(Write, Complete, () => Complete()));
        var dispose = false;
        lock (_lifecycleGate)
        {
            if (_state is Lifecycle.Disposing or Lifecycle.Disposed || _callbacksStopped)
            {
                dispose = true;
            }
            else
            {
                _subscription = subscription;
            }
        }
        if (dispose)
        {
            subscription.Dispose();
        }
    }

    Task BeginDisposal()
    {
        TaskCompletionSource completion;
        lock (_lifecycleGate)
        {
            if (_disposal is not null)
            {
                return _disposal.Task;
            }

            completion = _disposal = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _state = Lifecycle.Disposing;
            _callbacksStopped = true;
            if (_callbacks == 0)
            {
                _callbacksDrained.TrySetResult();
            }
            if (_reads == 0)
            {
                _readsDrained.TrySetResult();
            }
        }

        _ = CompleteDisposal(completion);
        return completion.Task;
    }

    async Task CompleteDisposal(TaskCompletionSource completion)
    {
        try
        {
            await _lifetime.CancelAsync().ConfigureAwait(false);
            await _startup.Task.ConfigureAwait(false);
            TakeSubscription()?.Dispose();
            await _enumeration.CancelAsync().ConfigureAwait(false);
            await _callbacksDrained.Task.ConfigureAwait(false);
            if (_producer is not null)
            {
                await _producer.ConfigureAwait(false);
            }
            await _readsDrained.Task.ConfigureAwait(false);
            await DrainProcessing().ConfigureAwait(false);
            if (_scope is IAsyncDisposable asyncScope)
            {
                await asyncScope.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                _scope?.Dispose();
            }

            ReleaseResources();
            completion.TrySetResult();
        }
        catch (Exception error)
        {
            SetDisposed();
            completion.TrySetException(error);
        }
    }

    void SetDisposed()
    {
        lock (_lifecycleGate)
        {
            _state = Lifecycle.Disposed;
        }
    }

    void ReleaseResources()
    {
        _readGate.Dispose();
        _lifetime.Dispose();
        _enumeration.Dispose();
        SetDisposed();
    }

    IDisposable? TakeSubscription()
    {
        lock (_lifecycleGate)
        {
            _callbacksStopped = true;
            var subscription = _subscription;
            _subscription = null;

            return subscription;
        }
    }

    async Task<QueryResult?> Process(Emission emission)
    {
        try
        {
            if (emission.IsTerminal)
            {
                return End(emission.Error is { } error
                    ? QueryResult.Error(admitted.CorrelationId, error)
                    : QueryResult.Error(admitted.CorrelationId, "Observable query completed before producing its first result."));
            }

            var principal = Context.AuthorizedPrincipal ?? new ClaimsPrincipal();
            using var identity = Services.GetRequiredService<CurrentPrincipalAccessor>().UseAuthorizationPrincipal(principal, Services);
            using var hostIdentity = Services.GetRequiredService<IAuthorizationPolicyRuntime>().BeginPrincipalScope(principal, Services);
            var data = await Services.GetRequiredService<IReadModelInterceptors>()
                .InterceptEmission(emission.Type!, emission.Data, Services).ConfigureAwait(false);
            var verdict = await Guard(_lifetime.Token).ConfigureAwait(false);

            return verdict switch
            {
                ObservableQueryEmissionVerdict.Suppress => null,
                ObservableQueryEmissionVerdict.DenyAndTerminate => End(QueryResult.Unauthorized(admitted.CorrelationId)),
                _ => new QueryResult
                {
                    CorrelationId = admitted.CorrelationId,
                    Data = materialize(data)!,
                    Paging = emission.Paging!
                }
            };
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            return End(QueryResult.Error(admitted.CorrelationId, error));
        }
    }

    async Task DrainProcessing()
    {
        try
        {
            if (_pending is not null)
            {
                await _pending.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            // Disposal cancelled the retained guard; wait for it to exit before releasing its scope.
            _pending = null;
        }
    }

    async Task<ObservableQueryEmissionVerdict> Guard(CancellationToken cancellationToken)
    {
        var guards = Services.GetRequiredService<IObservableQueryEmissionGuards>();
        if (!guards.HasGuards)
        {
            return ObservableQueryEmissionVerdict.Allow;
        }

        return await guards.Guard(new ObservableQueryEmissionContext(
            Context.Name,
            Context.Arguments ?? QueryArguments.Empty,
            Context.AuthorizedPrincipal,
            Context.CorrelationId,
            Services,
            !_delivered,
            cancellationToken)
        {
            SubscriptionScopeSnapshot = Context.SubscriptionScopeSnapshot
        }).ConfigureAwait(false);
    }

    QueryResult End(QueryResult result)
    {
        _terminal = result;
        _enumeration.Cancel();
        TakeSubscription()?.Dispose();

        return result;
    }

    bool EnterCallback()
    {
        lock (_lifecycleGate)
        {
            if (_callbacksStopped || _state is Lifecycle.Disposing or Lifecycle.Disposed)
            {
                return false;
            }

            _callbacks++;
            return true;
        }
    }

    void ExitCallback()
    {
        lock (_lifecycleGate)
        {
            _callbacks--;
            if (_state == Lifecycle.Disposing && _callbacks == 0)
            {
                _callbacksDrained.TrySetResult();
            }
        }
    }

    void Write<T>(T value)
    {
        if (!EnterCallback())
        {
            return;
        }

        try
        {
            _emissions.Writer.TryWrite(new(
                materialize(value),
                typeof(T),
                new PagingInfo(Context.Paging.Page, Context.Paging.Size, Context.TotalItems),
                false,
                null));
        }
        catch (Exception error)
        {
            Complete(error);
        }
        finally
        {
            ExitCallback();
        }
    }

    void Complete(Exception? error = null)
    {
        if (!EnterCallback())
        {
            return;
        }

        try
        {
            _emissions.Writer.TryWrite(new(null, null, null, true, error));
            _emissions.Writer.TryComplete();
        }
        finally
        {
            ExitCallback();
        }
    }

    async Task Produce<T>(IAsyncEnumerable<T> enumerable)
    {
        try
        {
            await foreach (var value in enumerable.WithCancellation(_enumeration.Token).ConfigureAwait(false))
            {
                Write(value);
            }
            Complete();
        }
        catch (OperationCanceledException) when (_enumeration.IsCancellationRequested)
        {
            // Disposal or a terminal guard verdict cancels enumeration; it is not a stream failure.
            _emissions.Writer.TryComplete();
        }
        catch (Exception error)
        {
            Complete(error);
        }
    }

    sealed record Emission(object? Data, Type? Type, PagingInfo? Paging, bool IsTerminal, Exception? Error);
}
