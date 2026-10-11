// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Cratis.DependencyInjection;
using Cratis.Execution;
using Cratis.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an implementation of <see cref="IObservableQueryDemultiplexer"/> providing composite observable
/// query streaming over a fixed WebSocket endpoint (<c>/.cratis/queries/ws</c>) and a fixed
/// Server-Sent Events endpoint (<c>/.cratis/queries/sse</c>).
/// </summary>
/// <remarks>
/// <para>
/// Authorization is honored for every subscription through the query pipeline filters. If the
/// current user is not authorized to perform a query, an
/// <see cref="ObservableQueryHubMessageType.Unauthorized"/> message is sent instead of data.
/// </para>
/// <para>
/// Both transports support multiple concurrent subscriptions over a single connection.
/// The WebSocket transport uses bidirectional frames; the SSE transport uses separate POST
/// endpoints for subscribe/unsubscribe, correlated via a server-assigned connection identifier.
/// </para>
/// <para>
/// Because the SSE transport spreads one connection across several HTTP requests, the caller that opened the
/// stream is captured as an <see cref="ObservableQueryCaller"/> and every subscribe and unsubscribe is checked
/// against it. A control request from a different caller is answered with 404, identically to one naming a
/// connection that does not exist, so a connection identifier on its own grants nothing.
/// </para>
/// <para>
/// Each subject-backed subscription gets its own <see cref="IServiceScope"/>, created at subscribe time from
/// <paramref name="serviceProvider"/> and disposed when the subscription ends. Chronicle's client services are
/// registered scoped, resolving the current tenant's namespace the first time they are used within a scope and
/// caching it for the scope's lifetime — resolving them from the root container instead would cache whichever
/// tenant asked first and reuse it for every subscription thereafter. Emissions arrive on the subject's own
/// producer thread, where the AsyncLocal tenant context does not flow, so <see cref="IHttpRequestContextAccessor.Current"/>
/// is restored to the subscribing connection immediately before each interception call.
/// </para>
/// <para>
/// The subscription-time authorization verdict gates <em>obtaining</em> the stream, not the stream itself. An
/// application that needs the verdict re-checked while a subscription is running implements an
/// <see cref="IGuardObservableQueryEmission"/>; it is then consulted for every emission, and can withhold a single
/// emission or terminate the subscription. With no guard implemented nothing is dispatched and emissions take exactly
/// the path they always did.
/// </para>
/// </remarks>
/// <param name="queryPipeline">The <see cref="IQueryPipeline"/> used to perform and authorize queries.</param>
/// <param name="queryContextManager">The <see cref="IQueryContextManager"/> for managing query contexts.</param>
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/> used to propagate the caller's identity into the authorization pipeline, and restored around each emission so tenant resolution sees the subscribing connection.</param>
/// <param name="hostApplicationLifetime">The <see cref="IHostApplicationLifetime"/> used to cancel connections on shutdown.</param>
/// <param name="readModelInterceptors">The <see cref="IReadModelInterceptors"/> used to intercept (e.g. decrypt compliance/PII properties on) each emitted read model before it is sent to the client.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to create a per-subscription <see cref="IServiceScope"/> for resolving interceptors — see remarks.</param>
/// <param name="arcOptions">The <see cref="ArcOptions"/> used for JSON serialization.</param>
/// <param name="healthTracker">The <see cref="IQueryHealthTracker"/> used to track subscription health.</param>
/// <param name="emissionGuards">The <see cref="IObservableQueryEmissionGuards"/> consulted per emission when an application opts in with an <see cref="IGuardObservableQueryEmission"/>.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public partial class ObservableQueryDemultiplexer(
    IQueryPipeline queryPipeline,
    IQueryContextManager queryContextManager,
    IHttpRequestContextAccessor httpRequestContextAccessor,
    IHostApplicationLifetime hostApplicationLifetime,
    IReadModelInterceptors readModelInterceptors,
    IServiceProvider serviceProvider,
    IOptions<ArcOptions> arcOptions,
    IQueryHealthTracker healthTracker,
    IObservableQueryEmissionGuards emissionGuards,
    ILogger<ObservableQueryDemultiplexer> logger) : IObservableQueryDemultiplexer
{
    const int WebSocketBufferSize = 1024 * 4;

    readonly ConcurrentDictionary<string, SSEConnectionState> _sseConnections = new();
    readonly ChangeSetComputor _changeSetComputor = new(arcOptions.Value.JsonSerializerOptions);
    int _nextConnectionId;

    /// <summary>
    /// Subscribes to streaming data without an owning multiplexed operation completion callback.
    /// </summary>
    /// <param name="context">The subscribing connection's <see cref="IHttpRequestContext"/>.</param>
    /// <param name="streamingData">The streaming result to subscribe to.</param>
    /// <param name="queryId">The id of the query the subscription is for.</param>
    /// <param name="paging">The <see cref="PagingInfo"/> to report with each result.</param>
    /// <param name="transferMode">The transfer mode, or null for the default.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> to stamp each result with.</param>
    /// <param name="identity">The subscription query and caller identity.</param>
    /// <param name="onNext">Callback invoked for each result.</param>
    /// <param name="onError">Callback invoked when the subscription errors.</param>
    /// <param name="onUnauthorized">Callback invoked when an emission guard denies the stream.</param>
    /// <param name="token">The connection cancellation token.</param>
    /// <returns>A disposable that stops the subscription, or null when the data is not streamable.</returns>
    internal IDisposable? SubscribeToStreamingData(
        IHttpRequestContext context,
        object streamingData,
        string queryId,
        PagingInfo paging,
        string? transferMode,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        Func<QueryResult, CancellationToken, Task> onNext,
        Func<string, string, CancellationToken, Task> onError,
        Func<string, CancellationToken, Task> onUnauthorized,
        CancellationToken token) =>
        SubscribeToStreamingData(
            context,
            streamingData,
            queryId,
            paging,
            transferMode,
            correlationId,
            identity,
            onNext,
            onError,
            onUnauthorized,
            () => { },
            token);

    /// <summary>
    /// Subscribes to a streaming query result — an <see cref="ISubject{T}"/> or an <see cref="IAsyncEnumerable{T}"/>
    /// — and returns a disposable that tears the subscription down.
    /// </summary>
    /// <param name="context">The subscribing connection's <see cref="IHttpRequestContext"/>.</param>
    /// <param name="streamingData">The streaming result to subscribe to.</param>
    /// <param name="queryId">The id of the query the subscription is for.</param>
    /// <param name="paging">The <see cref="PagingInfo"/> to report with each result.</param>
    /// <param name="transferMode">The transfer mode (for example delta or full), or null for the default.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> to stamp each result with.</param>
    /// <param name="identity">The <see cref="ObservableQuerySubscriptionIdentity"/> describing the query and the caller that established the subscription.</param>
    /// <param name="onNext">Callback invoked with each streamed <see cref="QueryResult"/> and the active subscription token.</param>
    /// <param name="onError">Callback invoked with the query id, message and active subscription token when the subscription errors.</param>
    /// <param name="onUnauthorized">Callback invoked with the query id and active subscription token when an emission guard denies the stream, ending the subscription.</param>
    /// <param name="onCompleted">Callback that terminates the exact owning operation when the stream completes.</param>
    /// <param name="token">The connection's <see cref="CancellationToken"/>.</param>
    /// <returns>An <see cref="IDisposable"/> that stops the subscription, or null when the data is not streamable.</returns>
    internal IDisposable? SubscribeToStreamingData(
        IHttpRequestContext context,
        object streamingData,
        string queryId,
        PagingInfo paging,
        string? transferMode,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        Func<QueryResult, CancellationToken, Task> onNext,
        Func<string, string, CancellationToken, Task> onError,
        Func<string, CancellationToken, Task> onUnauthorized,
        Action onCompleted,
        CancellationToken token) =>
        SubscribeToStreamingData(context, streamingData, queryId, paging, transferMode, correlationId, identity, onNext, onError, onUnauthorized, onCompleted, null, token);

    /// <summary>
    /// Subscribes a connection to a subject of a known element type.
    /// </summary>
    /// <typeparam name="T">The element type of the subject.</typeparam>
    /// <param name="context">The <see cref="IHttpRequestContext"/> of the connection.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="enumerableItemType">The item type when <typeparamref name="T"/> is or implements <see cref="IEnumerable{T}"/>; otherwise <see langword="null"/>.</param>
    /// <param name="queryId">The client's identifier for the subscription.</param>
    /// <param name="paging">The <see cref="PagingInfo"/> of the query.</param>
    /// <param name="transferMode">The requested transfer mode.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> of the query.</param>
    /// <param name="identity">The <see cref="ObservableQuerySubscriptionIdentity"/>.</param>
    /// <param name="onNext">Called for each result.</param>
    /// <param name="onError">Called when the subscription fails.</param>
    /// <param name="onUnauthorized">Called when the subscription is no longer authorized.</param>
    /// <param name="onCompleted">Called when the subject completes.</param>
    /// <param name="authorizedQueryContext">The <see cref="QueryContext"/> the query was authorized with, if any.</param>
    /// <param name="token">The <see cref="CancellationToken"/> ending the subscription.</param>
    /// <returns>The subscription.</returns>
    /// <remarks>Called by <see cref="ObservableQueryElementOperationsFor{TElement}"/> once the element type is known.</remarks>
    internal IDisposable SubscribeToTypedSubject<T>(
        IHttpRequestContext context,
        ISubject<T> subject,
        Type? enumerableItemType,
        string queryId,
        PagingInfo paging,
        string? transferMode,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        Func<QueryResult, CancellationToken, Task> onNext,
        Func<string, string, CancellationToken, Task> onError,
        Func<string, CancellationToken, Task> onUnauthorized,
        Action onCompleted,
        QueryContext? authorizedQueryContext,
        CancellationToken token) =>
        SubscribeToSubjectOfType(context, subject, enumerableItemType, queryId, paging, transferMode, correlationId, identity, onNext, onError, onUnauthorized, onCompleted, authorizedQueryContext, token);

    /// <summary>
    /// Streams an async enumerable of a known element type to a connection.
    /// </summary>
    /// <typeparam name="T">The element type of the async enumerable.</typeparam>
    /// <param name="context">The <see cref="IHttpRequestContext"/> of the connection.</param>
    /// <param name="enumerable">The async enumerable.</param>
    /// <param name="queryId">The client's identifier for the subscription.</param>
    /// <param name="paging">The <see cref="PagingInfo"/> of the query.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> of the query.</param>
    /// <param name="identity">The <see cref="ObservableQuerySubscriptionIdentity"/>.</param>
    /// <param name="guardServiceProvider">The <see cref="IServiceProvider"/> for emission guards, if any are registered.</param>
    /// <param name="onNext">Called for each result.</param>
    /// <param name="onError">Called when the stream fails.</param>
    /// <param name="onUnauthorized">Called when the stream is no longer authorized.</param>
    /// <param name="token">The <see cref="CancellationToken"/> ending the stream.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>Called by <see cref="ObservableQueryElementOperationsFor{TElement}"/> once the element type is known.</remarks>
    internal Task StreamTypedAsyncEnumerable<T>(
        IHttpRequestContext context,
        IAsyncEnumerable<T> enumerable,
        string queryId,
        PagingInfo paging,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        IServiceProvider? guardServiceProvider,
        Func<QueryResult, CancellationToken, Task> onNext,
        Func<string, string, CancellationToken, Task> onError,
        Func<string, CancellationToken, Task> onUnauthorized,
        CancellationToken token) =>
        StreamAsyncEnumerableOfType(context, enumerable, queryId, paging, correlationId, identity, guardServiceProvider, onNext, onError, onUnauthorized, token);

    static bool IsDisconnect(Exception exception, CancellationToken token) =>
        token.IsCancellationRequested &&
        exception is OperationCanceledException or IOException or ObjectDisposedException;

    static bool IsFatal(Exception exception) =>
        exception is OutOfMemoryException or
            StackOverflowException or
            AccessViolationException or
            AppDomainUnloadedException or
            BadImageFormatException or
            CannotUnloadAppDomainException or
            InvalidProgramException;

    ObservableQuerySubscriptionOperation? ReserveSubscription(
        ObservableQuerySubscriptionStates subscriptions,
        string queryId,
        long? revision,
        CancellationToken connectionToken) =>
        subscriptions.TrySubscribe(queryId, revision, connectionToken);

    bool IsCurrent(
        ObservableQuerySubscriptionStates subscriptions,
        string queryId,
        ObservableQuerySubscriptionOperation operation) =>
        subscriptions.IsCurrent(queryId, operation);

    void TerminateSubscription(
        ObservableQuerySubscriptionStates subscriptions,
        string queryId,
        ObservableQuerySubscriptionOperation operation) =>
        subscriptions.Terminate(queryId, operation);

    IDisposable? SubscribeToStreamingData(
        IHttpRequestContext context,
        object streamingData,
        string queryId,
        PagingInfo paging,
        string? transferMode,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        Func<QueryResult, CancellationToken, Task> onNext,
        Func<string, string, CancellationToken, Task> onError,
        Func<string, CancellationToken, Task> onUnauthorized,
        Action onCompleted,
        QueryContext? authorizedQueryContext,
        CancellationToken token)
    {
        var type = streamingData.GetType();

        if (type.ImplementsOpenGeneric(typeof(ISubject<>)))
        {
            return SubscribeToSubject(context, streamingData, type, queryId, paging, transferMode, correlationId, identity, onNext, onError, onUnauthorized, onCompleted, authorizedQueryContext, token);
        }

        if (type.ImplementsOpenGeneric(typeof(IAsyncEnumerable<>)))
        {
            var lifetime = new StreamingQuerySubscription(token);
            var guardScope = CreateGuardScope(lifetime);

            if (lifetime.TryEnter())
            {
                _ = RunStream();
            }

            return lifetime;

            async Task RunStream()
            {
                try
                {
                    await StreamAsyncEnumerable(
                        context,
                        streamingData,
                        type,
                        queryId,
                        paging,
                        correlationId,
                        identity,
                        guardScope?.ServiceProvider,
                        onNext,
                        onError,
                        onUnauthorized,
                        lifetime.Token);
                }
                finally
                {
                    try
                    {
                        onCompleted();
                    }
                    finally
                    {
                        lifetime.Exit();
                    }
                }
            }
        }

        return null;
    }

    IServiceScope? CreateGuardScope(StreamingQuerySubscription lifetime)
    {
        if (!emissionGuards.HasGuards)
        {
            return null;
        }

        var ownedScope = (IServiceScope?)serviceProvider.CreateScope();
        try
        {
            lifetime.AddResource(ownedScope);
            var guardScope = ownedScope;
            ownedScope = null;
            return guardScope;
        }
        finally
        {
            ownedScope?.Dispose();
        }
    }

    IDisposable SubscribeToSubject(
        IHttpRequestContext context,
        object subject,
        Type subjectType,
        string queryId,
        PagingInfo paging,
        string? transferMode,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        Func<QueryResult, CancellationToken, Task> onNext,
        Func<string, string, CancellationToken, Task> onError,
        Func<string, CancellationToken, Task> onUnauthorized,
        Action onCompleted,
        QueryContext? authorizedQueryContext,
        CancellationToken token)
    {
        var elementType = ObservableQueryElementOperations.FindElementType(subjectType, typeof(ISubject<>))!;
        return ObservableQueryElementOperations.For(elementType).SubscribeToSubject(
            this, context, subject, queryId, paging, transferMode, correlationId, identity, onNext, onError, onUnauthorized, onCompleted, authorizedQueryContext, token);
    }

    StreamingQuerySubscription SubscribeToSubjectOfType<T>(
        IHttpRequestContext context,
        ISubject<T> subject,
        Type? enumerableItemType,
        string queryId,
        PagingInfo paging,
        string? transferMode,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        Func<QueryResult, CancellationToken, Task> onNext,
        Func<string, string, CancellationToken, Task> onError,
        Func<string, CancellationToken, Task> onUnauthorized,
        Action onCompleted,
        QueryContext? authorizedQueryContext,
        CancellationToken token)
    {
        // The pipeline's context survives its AsyncLocal boundary. Keep the instance so provider updates to
        // TotalItems remain visible on later emissions; custom pipelines can still use the ambient context.
        var state = new SubjectEmissionState
        {
            QueryId = queryId,
            Identity = identity,
            CorrelationId = correlationId,
            Paging = paging,
            EnumerableItemType = enumerableItemType,
            TransferMode = transferMode,
            SubscriptionQueryContext = authorizedQueryContext ?? queryContextManager.Current
        };

        // Interception (compliance/PII release) is asynchronous. Emissions are serialized through this gate so
        // the per-emission interception and the ChangeSet's previous/current item bookkeeping stay ordered even
        // when the underlying subject delivers the next value before the previous one has finished interception.
        var emissionGate = new SemaphoreSlim(1, 1);

        var interceptionScope = serviceProvider.CreateScope();
        var lifetime = new StreamingQuerySubscription(token);
        AddOwnedResources(lifetime, emissionGate, interceptionScope);

        var subscriptionToken = lifetime.Token;
        try
        {
            // BehaviorSubject can replay synchronously from inside Subscribe. The lifetime therefore exists before
            // observer attachment, and SetProducer disposes an observer whose replay terminated the operation.
            lifetime.SetProducer(subject.Subscribe(OnEmission, OnSubscriptionError, OnSubscriptionCompleted));
        }
        catch
        {
            lifetime.Dispose();
            throw;
        }

        return lifetime;

        // This is an async void callback invoked by the subject on a background (ThreadPool) thread. Any
        // exception that escapes it is unobserved and terminates the whole process. The connection's
        // per-subscription resources (the emission gate, the write lock and the linked cancellation source)
        // are disposed the moment the client disconnects, and an emission already in flight then races against
        // that disposal — touching a disposed SemaphoreSlim or CancellationTokenSource throws
        // ObjectDisposedException. The entire body is therefore wrapped so that nothing can propagate: the
        // expected disconnect signals (cancellation, disposal, broken transport) become no-ops, and only a
        // genuine failure is surfaced to the subscriber.
        async void OnEmission(T data)
        {
            if (!lifetime.TryEnter())
            {
                return;
            }

            var gateAcquired = false;
            var previousContext = httpRequestContextAccessor.Current;
            try
            {
                await emissionGate.WaitAsync(subscriptionToken);
                gateAcquired = true;

                // An emission that was already queued behind the gate when a guard terminated the subscription must
                // not still be written — the client has been told it is no longer authorized.
                if (state.IsTerminated)
                {
                    return;
                }

                // Restore the subscribing connection's context — this callback runs on the subject's own
                // producer thread, where the AsyncLocal tenant context set up when the subscription was
                // created does not flow (see the class remarks).
                using var emissionIdentity = ObservableEmissionIdentity.Begin(
                    context, interceptionScope.ServiceProvider, identity.Principal ?? context.User, identity.AuthorizedTenant, accessor: httpRequestContextAccessor);
                var interceptedData = await readModelInterceptors.InterceptEmission(typeof(T), data, interceptionScope.ServiceProvider);
                subscriptionToken.ThrowIfCancellationRequested();

                if (!await AdmitSubjectEmission(state, interceptionScope.ServiceProvider, onUnauthorized, subscriptionToken))
                {
                    return;
                }

                var changes = ComputeEmissionChanges(state, data, interceptedData);
                var result = CreateSubjectResult(state, interceptedData, changes);

                subscriptionToken.ThrowIfCancellationRequested();
                await onNext(result, subscriptionToken);
                state.HasDeliveredEmission = true;
            }
            catch (Exception error) when (IsDisconnect(error, subscriptionToken))
            {
                logger.EmissionAfterDisconnect(queryId);
            }
            catch (Exception error) when (!IsFatal(error))
            {
                logger.SubscriptionError(queryId, error);
                if (await ReportErrorQuietly(queryId, error, onError, subscriptionToken))
                {
                    logger.EmissionAfterDisconnect(queryId);
                }
            }
            finally
            {
                httpRequestContextAccessor.Current = previousContext;
                if (gateAcquired)
                {
                    emissionGate.Release();
                }
                lifetime.Exit();
            }
        }

        async void OnSubscriptionError(Exception error)
        {
            if (!lifetime.TryEnter())
            {
                return;
            }

            var previousContext = httpRequestContextAccessor.Current;
            try
            {
                using var emissionIdentity = ObservableEmissionIdentity.Begin(
                    context, interceptionScope.ServiceProvider, identity.Principal ?? context.User, identity.AuthorizedTenant, accessor: httpRequestContextAccessor);
                if (subscriptionToken.IsCancellationRequested)
                {
                    return;
                }

                logger.SubscriptionError(queryId, error);
                await onError(queryId, error.Message, subscriptionToken);
            }
            catch (Exception callbackError) when (IsDisconnect(callbackError, subscriptionToken))
            {
                logger.EmissionAfterDisconnect(queryId);
            }
            catch (Exception callbackError) when (!IsFatal(callbackError))
            {
                logger.SubscriptionError(queryId, callbackError);
            }
            finally
            {
                httpRequestContextAccessor.Current = previousContext;
                try
                {
                    onCompleted();
                }
                finally
                {
                    lifetime.Exit();
                }
            }
        }

        void OnSubscriptionCompleted()
        {
            if (!lifetime.TryEnter())
            {
                return;
            }

            try
            {
                onCompleted();
            }
            finally
            {
                lifetime.Exit();
            }
        }
    }

    async Task StreamAsyncEnumerable(
        IHttpRequestContext context,
        object enumerable,
        Type enumerableType,
        string queryId,
        PagingInfo paging,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        IServiceProvider? guardServiceProvider,
        Func<QueryResult, CancellationToken, Task> onNext,
        Func<string, string, CancellationToken, Task> onError,
        Func<string, CancellationToken, Task> onUnauthorized,
        CancellationToken token)
    {
        var elementType = ObservableQueryElementOperations.FindElementType(enumerableType, typeof(IAsyncEnumerable<>))!;
        await ObservableQueryElementOperations.For(elementType).StreamAsyncEnumerable(
            this, context, enumerable, queryId, paging, correlationId, identity, guardServiceProvider, onNext, onError, onUnauthorized, token);
    }

    async Task StreamAsyncEnumerableOfType<T>(
        IHttpRequestContext context,
        IAsyncEnumerable<T> enumerable,
        string queryId,
        PagingInfo paging,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        IServiceProvider? guardServiceProvider,
        Func<QueryResult, CancellationToken, Task> onNext,
        Func<string, string, CancellationToken, Task> onError,
        Func<string, CancellationToken, Task> onUnauthorized,
        CancellationToken token)
    {
        var hasDeliveredEmission = false;
        var previousContext = httpRequestContextAccessor.Current;

        try
        {
            using var emissionIdentity = ObservableEmissionIdentity.Begin(
                context, guardServiceProvider ?? serviceProvider, identity.Principal ?? context.User, identity.AuthorizedTenant, accessor: httpRequestContextAccessor);
            await foreach (var item in enumerable.WithCancellation(token))
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                var outcome = await EvaluateGuard(
                    new GuardRequest(queryId, identity, correlationId, guardServiceProvider, !hasDeliveredEmission),
                    onUnauthorized,
                    token);
                if (outcome == GuardOutcome.Stop)
                {
                    return;
                }

                if (outcome == GuardOutcome.Skip)
                {
                    continue;
                }

                var result = CreateDeliverableResult(correlationId, item!, paging);

                token.ThrowIfCancellationRequested();
                await onNext(result, token);
                token.ThrowIfCancellationRequested();
                hasDeliveredEmission = true;
            }
        }
        catch (Exception ex) when (IsDisconnect(ex, token))
        {
            // The subscription ended while the stream or one of its callbacks was active.
        }
        catch (Exception ex) when (!IsFatal(ex))
        {
            logger.SubscriptionError(queryId, ex);
            await ReportErrorQuietly(queryId, ex, onError, token);
        }
        finally
        {
            httpRequestContextAccessor.Current = previousContext;
        }
    }

    void ReleaseWriteLock(SemaphoreSlim writeLock, CancellationToken token)
    {
        try
        {
            writeLock.Release();
        }
        catch (ObjectDisposedException) when (token.IsCancellationRequested)
        {
            // The write lock was disposed after connection cancellation.
        }
        catch (ObjectDisposedException ex)
        {
            logger.ErrorSendingMessage(ex);
        }
    }

    /// <summary>
    /// Runs the transport-agnostic keep-alive loop, guaranteeing that no more than the configured
    /// interval passes between messages sent to the client.
    /// </summary>
    /// <param name="keepAliveTracker">The <see cref="KeepAliveTracker"/> recording when messages were last sent.</param>
    /// <param name="sendKeepAlive">Sends a keep-alive message over the transport.</param>
    /// <param name="token">The <see cref="CancellationToken"/> that ends the loop.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The loop waits until a keep-alive is actually due relative to the last message sent, rather than
    /// waking on a fixed interval grid. A fixed grid defers the keep-alive to the next tick whenever a
    /// data message lands mid-interval, which lets the gap between messages grow to nearly twice the
    /// interval and causes clients watching for silence to drop an otherwise healthy connection.
    /// </remarks>
    async Task RunKeepAlive(KeepAliveTracker keepAliveTracker, Func<Task> sendKeepAlive, CancellationToken token)
    {
        var interval = arcOptions.Value.Query.KeepAliveInterval;

        if (interval <= TimeSpan.Zero)
        {
            return;
        }

        try
        {
            while (!token.IsCancellationRequested)
            {
                var remaining = keepAliveTracker.GetTimeUntilNextKeepAlive(interval);

                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining, token);
                    continue;
                }

                await sendKeepAlive();

                // A successful send records activity, so the next iteration waits a full interval.
                // If the send failed without recording anything, wait one interval anyway so that a
                // persistently failing transport cannot spin this loop.
                if (keepAliveTracker.GetTimeUntilNextKeepAlive(interval) <= TimeSpan.Zero)
                {
                    await Task.Delay(interval, token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown — nothing to report.
        }
    }
}
