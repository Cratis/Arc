// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Cratis.Reflection;
using Cratis.Strings;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries;

public partial class ObservableQueryDemultiplexer
{
    static async Task NotifyAndComplete(SubscriptionCallbacks callbacks, Func<Task> notify)
    {
        try
        {
            await notify();
        }
        finally
        {
            callbacks.OnCompleted();
        }
    }

    static void EnsureAuthorizationDoesNotRequireAsynchronousEvaluation(FullyQualifiedQueryName fullyQualifiedName, IServiceProvider queryServiceProvider)
    {
        var authorization = FindQueryAuthorization(fullyQualifiedName, queryServiceProvider);
        if (authorization is null)
        {
            return;
        }

        var (performer, declarations) = authorization.Value;
        var target = QueryAuthorizationTarget.For(performer, declarations);
        var declaration = target is System.Reflection.MethodInfo method ? declarations.For(method) : declarations.For((Type)target);
        if (declaration.RequiresAsynchronousEvaluation)
        {
            throw new InvalidAuthorizationConfiguration($"Query '{fullyQualifiedName}' requires an Arc pipeline that prepares authorization before subscription.");
        }
    }

    static (IQueryPerformer Performer, AuthorizationDeclarations Declarations)? FindQueryAuthorization(FullyQualifiedQueryName fullyQualifiedName, IServiceProvider queryServiceProvider)
    {
        var catalog = queryServiceProvider.GetService<IQueryPerformerProviders>();
        if (catalog is null || !catalog.TryGetPerformersFor(fullyQualifiedName, out var performer))
        {
            return null;
        }

        var declarations = queryServiceProvider.GetService<AuthorizationDeclarations>();
        return declarations is null ? null : (performer, declarations);
    }

    static void SelectAuthorizedPrincipal(QueryResult queryResult, IHttpRequestContext context, IServiceScope? ownedScope)
    {
        if (queryResult.AuthorizedPrincipal is { } selectedPrincipal &&
            context is ObservableQuerySubscriptionHttpRequestContext subscriptionContext)
        {
            subscriptionContext.SelectAuthorizedPrincipal(selectedPrincipal, ownedScope?.ServiceProvider);
        }
    }

    static QueryResult CreateSnapshotResult(QueryResult queryResult) => new()
    {
        CorrelationId = queryResult.CorrelationId,
        Data = queryResult.Data,
        IsAuthorized = true,
        ValidationResults = [],
        ExceptionMessages = [],
        ExceptionStackTrace = string.Empty,
        Paging = queryResult.Paging
    };

    static Task SendValidationRejection(QueryResult result, SubscriptionCallbacks callbacks, CancellationToken token)
    {
        var rejection = new QueryResult
        {
            CorrelationId = result.CorrelationId,
            ValidationResults = result.ValidationResults
        };
        return callbacks.OnNext(rejection, token);
    }

    static bool TransferScopeToSubscription(IDisposable? subscription, IServiceScope? ownedScope)
    {
        if (subscription is StreamingQuerySubscription lifetime && ownedScope is not null)
        {
            lifetime.AddResource(ownedScope);
            return true;
        }

        return false;
    }

    static bool IsStreamingResult(object data) =>
        data.GetType().ImplementsOpenGeneric(typeof(ISubject<>)) ||
        data.GetType().ImplementsOpenGeneric(typeof(IAsyncEnumerable<>));

    static Paging BuildPaging(ObservableQuerySubscriptionRequest request)
    {
        if (request.PageSize.HasValue)
        {
            return new Paging(request.Page ?? 0, request.PageSize.Value, true);
        }

        return Paging.NotPaged;
    }

    static Sorting BuildSorting(ObservableQuerySubscriptionRequest request)
    {
        if (!string.IsNullOrEmpty(request.SortBy) && !string.IsNullOrEmpty(request.SortDirection))
        {
            var direction = request.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? SortDirection.Descending
                : SortDirection.Ascending;

            return new Sorting(request.SortBy.ToPascalCase(), direction);
        }

        return Sorting.None;
    }

    static QueryArguments BuildQueryArguments(IDictionary<string, string?>? arguments)
    {
        if (arguments is null)
        {
            return QueryArguments.Empty;
        }

        var result = new QueryArguments();
        foreach (var kvp in arguments)
        {
            if (kvp.Value is not null)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Creates the subscription for a reserved operation and wires it to the transport, registering it for health tracking.
    /// </summary>
    /// <param name="session">The <see cref="SubscriptionSession"/> describing the reserved operation and its transport.</param>
    /// <param name="requestContext">The live admission request; not retained by the session.</param>
    /// <returns>True when the subscription ended because the caller was not authorized; otherwise false.</returns>
    async Task<bool> EstablishSubscription(SubscriptionSession session, IHttpRequestContext requestContext)
    {
        var operation = session.Operation;
        var previousContext = httpRequestContextAccessor.Current;

        try
        {
            httpRequestContextAccessor.Current = session.SubscriptionContext;

            var subscription = await CreateSubscription(
                new SubscriptionTarget(
                    session.SubscriptionContext,
                    requestContext,
                    session.SubscriptionContext.GetPrincipal(),
                    session.QueryId,
                    session.Request),
                new SubscriptionCallbacks(
                    (result, operationToken) => DeliverResult(session, result, operationToken),
                    (id, errorMsg, operationToken) => DeliverError(session, id, errorMsg, operationToken),
                    (id, operationToken) => DeliverUnauthorized(session, id, operationToken),
                    () => TerminateSubscription(session.Subscriptions, session.QueryId, operation)),
                operation.Token);

            AttachSubscription(session, subscription);
        }
        catch (Exception ex) when (IsDisconnect(ex, operation.Token))
        {
            // The operation was replaced or the connection ended while creation was in flight.
        }
        finally
        {
            httpRequestContextAccessor.Current = previousContext;
            if (operation.Token.IsCancellationRequested)
            {
                TerminateSubscription(session.Subscriptions, session.QueryId, operation);
            }
            operation.CompleteCreation();
        }

        return session.WasUnauthorized;
    }

    async Task DeliverResult(SubscriptionSession session, QueryResult result, CancellationToken operationToken)
    {
        if (!IsCurrent(session.Subscriptions, session.QueryId, session.Operation))
        {
            return;
        }

        var msg = ObservableQueryHubMessage.CreateQueryResult(session.QueryId, result, session.Operation.Revision);
        if (await session.Send(msg, operationToken) &&
            IsCurrent(session.Subscriptions, session.QueryId, session.Operation))
        {
            healthTracker.RecordDataServed(session.ConnectionId, session.QueryId);
        }
    }

    async Task DeliverError(SubscriptionSession session, string id, string errorMsg, CancellationToken operationToken)
    {
        if (!IsCurrent(session.Subscriptions, id, session.Operation))
        {
            return;
        }

        var msg = ObservableQueryHubMessage.CreateError(id, errorMsg, session.Operation.Revision);
        await session.Send(msg, operationToken);
    }

    async Task DeliverUnauthorized(SubscriptionSession session, string id, CancellationToken operationToken)
    {
        if (!IsCurrent(session.Subscriptions, id, session.Operation))
        {
            return;
        }

        operationToken.ThrowIfCancellationRequested();
        session.WasUnauthorized = true;
        var msg = ObservableQueryHubMessage.CreateUnauthorized(id, session.Operation.Revision);
        await session.Send(msg, operationToken);

        TerminateSubscription(session.Subscriptions, id, session.Operation);
    }

    bool TryAttachCurrent(SubscriptionSession session, IDisposable subscription) =>
        session.Operation.TryAttach(subscription) &&
        IsCurrent(session.Subscriptions, session.QueryId, session.Operation);

    void AttachSubscription(SubscriptionSession session, IDisposable? subscription)
    {
        if (subscription is not null && TryAttachCurrent(session, subscription))
        {
            var metadata = CreateSubscriptionMetadata(session.QueryId, session.Request, session.SubscriptionContext, session.Protocol);
            session.Operation.TryRegister(
                () => healthTracker.RegisterSubscription(session.ConnectionId, session.Protocol, metadata),
                () => healthTracker.UnregisterSubscription(session.ConnectionId, session.QueryId));
        }
        else if (subscription is null)
        {
            TerminateSubscription(session.Subscriptions, session.QueryId, session.Operation);
        }
    }

    async Task<IDisposable?> CreateSubscription(SubscriptionTarget target, SubscriptionCallbacks callbacks, CancellationToken token)
    {
        var queryId = target.QueryId;
        try
        {
            return await CreateSubscriptionCore(target, callbacks, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidAuthorizationConfiguration exception)
        {
            logger.ErrorProcessingMessage(exception);
            await NotifyAndComplete(callbacks, () => callbacks.OnUnauthorized(queryId, token));
            return null;
        }
        catch (Exception exception)
        {
            logger.ErrorProcessingMessage(exception);
            await NotifyAndComplete(callbacks, () => callbacks.OnError(queryId, "An error occurred while preparing the subscription.", token));
            return null;
        }
    }

    async Task<IDisposable?> CreateSubscriptionCore(SubscriptionTarget target, SubscriptionCallbacks callbacks, CancellationToken token)
    {
        var (context, requestContext, principal, queryId, request) = target;
        var queryServiceProvider = requestContext.RequestServices;
        var query = new SubscriptionQuery(
            new FullyQualifiedQueryName(request.QueryName),
            BuildQueryArguments(request.Arguments),
            BuildPaging(request),
            BuildSorting(request));

        if (!await AdmitHostFallback(query.Name, requestContext, principal, callbacks, queryId, token))
        {
            return null;
        }

        // Run through the full query pipeline (including authorization filters). The call stays inline: a custom
        // pipeline may set the query context (AsyncLocal) synchronously, and an async helper would restore it on return.
        QueryResult queryResult;
        if (queryPipeline is QueryPipeline builtInPipeline)
        {
            queryResult = await builtInPipeline.PerformHosted(query.Name, query.Arguments, query.Paging, query.Sorting, queryServiceProvider, token);
        }
        else
        {
            EnsureAuthorizationDoesNotRequireAsynchronousEvaluation(query.Name, queryServiceProvider);

            using var forwardedReceipt = OperationContextScope.ForwardTransportReceipt();
            queryResult = await queryPipeline.Perform(query.Name, query.Arguments, query.Paging, query.Sorting, queryServiceProvider, token);
        }

        var ownedScope = queryResult.OwnedScope;
        try
        {
            if (!queryResult.IsAuthorized)
            {
                logger.QueryUnauthorized(request.QueryName, queryId);
                await callbacks.OnUnauthorized(queryId, token);
                return null;
            }

            if (!queryResult.IsValid)
            {
                await SendValidationRejection(queryResult, callbacks, token);
                return null;
            }

            if (!queryResult.IsSuccess)
            {
                var errorMsg = string.Join("; ", queryResult.ExceptionMessages);
                await callbacks.OnError(queryId, errorMsg, token);
                return null;
            }

            SelectAuthorizedPrincipal(queryResult, context, ownedScope);

            var streamingData = queryResult.Data;
            if (streamingData is null || !IsStreamingResult(streamingData))
            {
                if (emissionGuards.HasGuards)
                {
                    var snapshotIdentity = CreateSubscriptionIdentity(query, principal, queryResult, queryServiceProvider);
                    var guardServices = ownedScope?.ServiceProvider ?? queryServiceProvider;
                    var guardRequest = new GuardRequest(queryId, snapshotIdentity, queryResult.CorrelationId, guardServices, IsFirstEmission: true, IsSnapshot: true);
                    if (await EvaluateGuard(guardRequest, callbacks.OnUnauthorized, token) != GuardOutcome.Deliver)
                    {
                        return null;
                    }
                }

                // A snapshot has no continuing subscription, including when a guard suppresses its only frame.
                await callbacks.OnNext(CreateSnapshotResult(queryResult), token);
                return null;
            }

            var identity = CreateSubscriptionIdentity(query, principal, queryResult, queryServiceProvider);
            IDisposable? subscription = null;
            try
            {
                subscription = SubscribeToStreamingData(context, streamingData, queryId, queryResult.Paging, request.TransferMode, queryResult.CorrelationId, identity, callbacks.OnNext, callbacks.OnError, callbacks.OnUnauthorized, callbacks.OnCompleted, queryResult.AuthorizedQueryContext, token);
                if (TransferScopeToSubscription(subscription, ownedScope))
                {
                    ownedScope = null;
                }

                var attached = subscription;
                subscription = null;
                return attached;
            }
            finally
            {
                subscription?.Dispose();
            }
        }
        finally
        {
            ownedScope?.Dispose();
        }
    }

    async Task<bool> AdmitHostFallback(
        FullyQualifiedQueryName queryName,
        IHttpRequestContext requestContext,
        ClaimsPrincipal principal,
        SubscriptionCallbacks callbacks,
        string queryId,
        CancellationToken token)
    {
        try
        {
            if (AllowsAnonymousQuery(queryName, requestContext.RequestServices))
            {
                return true;
            }

            var fallback = requestContext.RequestServices?.GetService<IEndpointFallbackAuthorization>() ??
                serviceProvider.GetRequiredService<IEndpointFallbackAuthorization>();
            if (await fallback.IsAuthorized(requestContext, principal, token))
            {
                token.ThrowIfCancellationRequested();
                return true;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (!IsFatal(exception))
        {
            // An unavailable host policy is a denial, never permission to execute the query.
            logger.HostFallbackCheckFailed(queryName.Value, queryId, exception);
        }

        await callbacks.OnUnauthorized(queryId, token);
        return false;
    }

    bool AllowsAnonymousQuery(FullyQualifiedQueryName queryName, IServiceProvider requestServices)
    {
        if (_anonymousQueries.TryGetValue(queryName, out var anonymous))
        {
            return anonymous;
        }

        var catalog = requestServices?.GetService<IQueryPerformerProviders>() ?? serviceProvider.GetService<IQueryPerformerProviders>();
        if (catalog is null || !catalog.TryGetPerformersFor(queryName, out var performer))
        {
            return false;
        }

        return _anonymousQueries.GetOrAdd(
            queryName,
            static (_, state) =>
            {
                using var scope = state.ServiceProvider.CreateScope();
                return QueryEndpointAccess.AllowsAnonymous(state.Performer, scope.ServiceProvider);
            },
            (ServiceProvider: serviceProvider, Performer: performer));
    }

    /// <summary>
    /// Creates the identity a subscription keeps for as long as it lives.
    /// </summary>
    /// <param name="query">The <see cref="SubscriptionQuery"/> that was requested.</param>
    /// <param name="principal">The principal that established the subscription.</param>
    /// <param name="queryResult">The result the pipeline produced.</param>
    /// <param name="queryServiceProvider">The <see cref="IServiceProvider"/> of the request.</param>
    /// <returns>The <see cref="ObservableQuerySubscriptionIdentity"/>.</returns>
    /// <remarks>
    /// The built-in pipeline returns its selected principal and coerced arguments explicitly. Legacy custom
    /// pipelines can still publish arguments through the context manager, but an AsyncLocal is not relied on
    /// for the built-in path and no request services or context are retained by the identity.
    /// </remarks>
    ObservableQuerySubscriptionIdentity CreateSubscriptionIdentity(
        SubscriptionQuery query,
        ClaimsPrincipal principal,
        QueryResult queryResult,
        IServiceProvider queryServiceProvider)
    {
        var legacyContext = queryResult.AuthorizedArguments is null ? queryContextManager.Current : null;
        return new ObservableQuerySubscriptionIdentity(
            query.Name,
            queryResult.AuthorizedArguments ?? legacyContext?.Arguments ?? query.Arguments,
            queryResult.AuthorizedPrincipal ?? principal,
            arcOptions.Value.JsonSerializerOptions)
        {
            AuthorizedTenant = queryResult.AuthorizedTenant ?? queryServiceProvider.GetService<TenantIdAccessor>()?.Current,
            SubscriptionScopeSnapshot = queryResult.AuthorizedQueryContext?.SubscriptionScopeSnapshot
        };
    }

    ObservableQuerySubscriptionRequest? DeserializeSubscriptionRequest(object? payload)
    {
        if (payload is null)
        {
            return null;
        }

        if (payload is ObservableQuerySubscriptionRequest request)
        {
            return request;
        }

        // When deserialized from JSON, the payload will be a JsonElement
        if (payload is JsonElement element)
        {
            return element.Deserialize(arcOptions.Value.JsonSerializerOptions.ResolveTypeInfo<ObservableQuerySubscriptionRequest>());
        }

        try
        {
            var json = JsonSerializer.Serialize(payload, arcOptions.Value.JsonSerializerOptions.ResolveTypeInfo<object>());
            return JsonSerializer.Deserialize(json, arcOptions.Value.JsonSerializerOptions.ResolveTypeInfo<ObservableQuerySubscriptionRequest>());
        }
        catch
        {
            return null;
        }
    }

    QuerySubscriptionMetadata CreateSubscriptionMetadata(
        string subscriptionId,
        ObservableQuerySubscriptionRequest request,
        ObservableQuerySubscriptionHttpRequestContext context,
        string protocol)
    {
        var lastDotIndex = request.QueryName.LastIndexOf('.');
        var readModelType = lastDotIndex >= 0 ? request.QueryName[..lastDotIndex] : request.QueryName;

        return new QuerySubscriptionMetadata
        {
            SubscriptionId = subscriptionId,
            QueryIdentifier = request.QueryName,
            ReadModelType = readModelType,
            ConnectedAt = DateTimeOffset.UtcNow,
            ClientInfo = new QuerySubscriptionClientInfo
            {
                RemoteIpAddress = context.Headers.GetValueOrDefault("X-Forwarded-For") ??
                                  context.Headers.GetValueOrDefault("X-Real-IP") ??
                                  "unknown",
                UserAgent = context.Headers.GetValueOrDefault("User-Agent"),
                UserId = context.User?.Identity?.Name,
                Protocol = protocol
            }
        };
    }

    readonly record struct SubscriptionCallbacks(
        Func<QueryResult, CancellationToken, Task> OnNext,
        Func<string, string, CancellationToken, Task> OnError,
        Func<string, CancellationToken, Task> OnUnauthorized,
        Action OnCompleted);

    readonly record struct SubscriptionTarget(
        IHttpRequestContext Context,
        IHttpRequestContext RequestContext,
        ClaimsPrincipal Principal,
        string QueryId,
        ObservableQuerySubscriptionRequest Request);

    readonly record struct SubscriptionQuery(
        FullyQualifiedQueryName Name,
        QueryArguments Arguments,
        Paging Paging,
        Sorting Sorting);

    sealed class SubscriptionSession
    {
        public required ObservableQuerySubscriptionHttpRequestContext SubscriptionContext { get; init; }
        public required ObservableQuerySubscriptionStates Subscriptions { get; init; }
        public required string ConnectionId { get; init; }
        public required string QueryId { get; init; }
        public required ObservableQuerySubscriptionRequest Request { get; init; }
        public required ObservableQuerySubscriptionOperation Operation { get; init; }
        public required string Protocol { get; init; }
        public required Func<ObservableQueryHubMessage, CancellationToken, Task<bool>> Send { get; init; }
        public bool WasUnauthorized { get; set; }
    }
}
