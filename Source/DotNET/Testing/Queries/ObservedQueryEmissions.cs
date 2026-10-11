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
/// Buffers producer-side snapshots and processes emissions sequentially on the reader side.
/// </summary>
/// <param name="admitted">The admitted query and its owned execution scope.</param>
/// <param name="materialize">Materializes collections before their producing scope can change.</param>
internal sealed class ObservedQueryEmissions(QueryResult admitted, Func<object?, object?> materialize) : IDisposable, IAsyncDisposable
{
    readonly object _callbackGate = new();
    readonly Channel<Emission> _emissions = Channel.CreateUnbounded<Emission>();
    readonly CancellationTokenSource _lifetime = new();
    readonly CancellationTokenSource _enumeration = new();
    readonly SemaphoreSlim _readGate = new(1, 1);
    IDisposable? _subscription;
    Task? _producer;
    Task<QueryResult?>? _pending;
    QueryResult? _terminal;
    bool _delivered;
    bool _callbacksStopped;
    int _disposed;

    QueryContext Context => admitted.AuthorizedQueryContext!;
    IServiceProvider Services => admitted.OwnedScope?.ServiceProvider ?? Context.ServiceProvider!;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _lifetime.Cancel();
        Stop();
        _producer?.GetAwaiter().GetResult();
        _readGate.Wait();
        try
        {
            DrainProcessing().GetAwaiter().GetResult();
            admitted.OwnedScope?.Dispose();
        }
        finally
        {
            ReleaseResources();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        await _lifetime.CancelAsync().ConfigureAwait(false);
        Stop();
        if (_producer is not null)
        {
            await _producer.ConfigureAwait(false);
        }
        await _readGate.WaitAsync().ConfigureAwait(false);
        try
        {
            await DrainProcessing().ConfigureAwait(false);
            if (admitted.OwnedScope is IAsyncDisposable asyncScope)
            {
                await asyncScope.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                admitted.OwnedScope?.Dispose();
            }
        }
        finally
        {
            ReleaseResources();
        }
    }

    /// <summary>
    /// Subscribes without running asynchronous work on the producer's thread.
    /// </summary>
    /// <typeparam name="T">The emitted value type.</typeparam>
    /// <param name="observable">The query's observable.</param>
    internal void Subscribe<T>(IObservable<T> observable) => _subscription = observable.Subscribe(Observer.Create<T>(
        value => Write(value),
        error => Complete(error),
        () => Complete()));

    /// <summary>
    /// Starts buffering an asynchronous query enumeration.
    /// </summary>
    /// <typeparam name="T">The emitted value type.</typeparam>
    /// <param name="enumerable">The query's asynchronous stream.</param>
    internal void Enumerate<T>(IAsyncEnumerable<T> enumerable) => _producer = Produce(enumerable);

    /// <summary>
    /// Reads and intercepts the next non-suppressed emission.
    /// </summary>
    /// <param name="timeout">The read deadline.</param>
    /// <param name="cancellationToken">Cancellation for this read.</param>
    /// <returns>The emission or a terminal query result.</returns>
    /// <exception cref="TimeoutException">No emission arrives before the deadline.</exception>
    internal async Task<QueryResult> Read(TimeSpan timeout, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        deadline.CancelAfter(timeout);
        var entered = false;
        try
        {
            await _readGate.WaitAsync(deadline.Token);
            entered = true;
            if (_terminal is not null)
            {
                return _terminal;
            }

            while (true)
            {
                _pending ??= Process(await _emissions.Reader.ReadAsync(deadline.Token));
                var result = await _pending.WaitAsync(deadline.Token);
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
        catch (Exception error) when (error is not OperationCanceledException)
        {
            return End(QueryResult.Error(admitted.CorrelationId, error));
        }
        finally
        {
            if (entered)
            {
                _readGate.Release();
            }
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
                .InterceptEmission(emission.Type!, emission.Data, Services);
            var verdict = await Guard(_lifetime.Token);

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
        });
    }

    QueryResult End(QueryResult result)
    {
        _terminal = result;
        Stop();

        return result;
    }

    void ReleaseResources()
    {
        _readGate.Release();
        _readGate.Dispose();
        _lifetime.Dispose();
        _enumeration.Dispose();
    }

    void Stop()
    {
        _enumeration.Cancel();
        _subscription?.Dispose();
        lock (_callbackGate)
        {
            _callbacksStopped = true;
        }
    }

    void Write<T>(T value)
    {
        lock (_callbackGate)
        {
            if (_callbacksStopped || Volatile.Read(ref _disposed) != 0)
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
        }
    }

    void Complete(Exception? error = null)
    {
        lock (_callbackGate)
        {
            if (_callbacksStopped || Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            _emissions.Writer.TryWrite(new(null, null, null, true, error));
            _emissions.Writer.TryComplete();
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
