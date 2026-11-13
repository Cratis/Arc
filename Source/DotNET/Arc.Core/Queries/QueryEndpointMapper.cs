// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Authorization;
using Cratis.Arc.EndpointGroups;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Cratis.Execution;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries;

/// <summary>
/// Maps query endpoints using the provided endpoint mapper.
/// </summary>
public static class QueryEndpointMapper
{
    /// <summary>
    /// Maps all query endpoints.
    /// </summary>
    /// <param name="mapper">The <see cref="IEndpointMapper"/> to use.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    public static void MapQueryEndpoints(this IEndpointMapper mapper, IServiceProvider serviceProvider)
    {
        var arcOptions = serviceProvider.GetRequiredService<IOptions<ArcOptions>>().Value;
        var options = arcOptions.GeneratedApis;
        var queryPerformerProviders = serviceProvider.GetRequiredService<IQueryPerformerProviders>();

        // A reader per supported transport (GET query string, QUERY body, …). Adding a transport is a
        // new IQueryRequestReader — this mapper stays untouched.
        var readers = serviceProvider.GetRequiredService<IInstancesOf<IQueryRequestReader>>()
            .Where(reader => options.EnableQueryHttpMethod || IsGet(reader))
            .ToArray();

        var performersByNamespace = EndpointRouteHelper.GroupByNamespace(
            queryPerformerProviders.Performers,
            p => p.Location,
            options.SegmentsToSkipForRoute);

        // Register public performers first so they win over internal performers when URLs conflict.
        var orderedPerformers = queryPerformerProviders.Performers
            .OrderByDescending(IsPublic);
        var registeredUrls = new Dictionary<string, IQueryPerformer>(StringComparer.OrdinalIgnoreCase);
        var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(QueryEndpointMapper).FullName!) ?? NullLogger.Instance;

        foreach (var performer in orderedPerformers)
        {
            var endpointGroup = performer.EnsureEndpointGroupServed(arcOptions.EndpointGroups);
            string url;
            IEnumerable<string> locationForTag;
            if (!string.IsNullOrEmpty(performer.CustomRoute))
            {
                // Use custom route if specified via Route attribute
                url = performer.CustomRoute;
                locationForTag = performer.Location.Skip(options.SegmentsToSkipForRoute);
            }
            else
            {
                // Use conventional route generation
                var location = performer.Location.Skip(options.SegmentsToSkipForRoute);
                var includeQueryName = EndpointRouteHelper.ShouldIncludeNameInRoute(
                    options.IncludeQueryNameInRoute,
                    location,
                    performersByNamespace);
                url = EndpointRouteHelper.BuildRouteUrl(arcOptions.EndpointGroups.RoutePrefixFor(endpointGroup, options.RoutePrefix), performer.Location, options.SegmentsToSkipForRoute, performer.Name.ToString(), includeQueryName);
                locationForTag = location;
            }

            if (!registeredUrls.TryAdd(url, performer))
            {
                ReportRouteCollision(logger, performer, url, registeredUrls[url]);
                continue;
            }

            foreach (var reader in readers)
            {
                MapForReader(mapper, new(reader, performer, new(url, locationForTag, endpointGroup)), serviceProvider);
            }
        }
    }

    static bool IsPublic(IQueryPerformer performer) =>
        performer.ReadModelType is { IsPublic: true } or { IsNestedPublic: true };

    static void ReportRouteCollision(ILogger logger, IQueryPerformer skipped, string url, IQueryPerformer mapped)
    {
        if (IsPublic(skipped))
        {
            logger.PublicQueryRouteCollision(skipped.FullyQualifiedName, url, mapped.FullyQualifiedName);
            return;
        }

        logger.InternalQueryRouteCollision(skipped.FullyQualifiedName, url, mapped.FullyQualifiedName);
    }

    static void MapForReader(IEndpointMapper mapper, QueryEndpointTarget target, IServiceProvider services)
    {
        var (reader, performer, placement) = target;
        if (mapper.EndpointExists(EndpointNameFor(reader, performer)))
        {
            return;
        }

        mapper.MapMethod(
            reader.HttpMethod,
            placement.Url,
            context => HandleRequest(context, reader, performer),
            BuildMetadata(target, services));
    }

    static string EndpointNameFor(IQueryRequestReader reader, IQueryPerformer performer) =>
        $"{reader.EndpointNamePrefix}{performer.FullyQualifiedName}";

    static EndpointMetadata BuildMetadata(QueryEndpointTarget target, IServiceProvider services)
    {
        var (reader, performer, placement) = target;
        return new EndpointMetadata(
            EndpointNameFor(reader, performer),
            $"{reader.EndpointNamePrefix} {performer.Name} query",
            [.. new[] { string.Join('.', placement.LocationForTag) }.Where(tag => !string.IsNullOrEmpty(tag))],
            QueryEndpointAccess.AllowsAnonymous(performer, services),
            RequestBodyType: reader.RequestBodyType,
            ResponseType: typeof(QueryResult),
            ExcludeFromApiDescription: placement.EndpointGroup is not null || !reader.IncludeInApiDescription || performer.IsExcludedFromDiscovery())
        {
            EndpointGroup = placement.EndpointGroup
        };
    }

    static ILogger GetLogger(IHttpRequestContext context) =>
        context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(QueryEndpointMapper).FullName!);

    static async Task HandleRequest(IHttpRequestContext context, IQueryRequestReader reader, IQueryPerformer performer)
    {
        using var receipt = OperationContextScope.Begin(context.RequestServices);
        var correlationIdAccessor = context.RequestServices.GetRequiredService<ICorrelationIdAccessor>();
        var arcOptions = context.RequestServices.GetRequiredService<IOptions<ArcOptions>>().Value;

        context.HandleCorrelationId(correlationIdAccessor, arcOptions.CorrelationId);

        if (reader.ResponseCacheControl is { } cacheControl)
        {
            context.SetResponseHeader("Cache-Control", cacheControl);
        }

        QueryRequest request;
        try
        {
            request = await reader.Read(context, performer);
        }
        catch (Exception ex)
        {
            await WriteReadFailure(context, ex);
            return;
        }

        await ProcessQuery(context, performer, request);
    }

    static async Task WriteReadFailure(IHttpRequestContext context, Exception ex)
    {
        // A reader rejecting the request as invalid client input carries its own validation result, which
        // is what names the reason on the wire. Everything else stays an exception result.
        var arcOptions = context.RequestServices.GetRequiredService<IOptions<ArcOptions>>().Value;
        var correlationId = context.RequestServices.GetRequiredService<ICorrelationIdAccessor>().Current;
        var errorResult = QueryResult.FromException(correlationId, ex);
        ExceptionDetailRedactor.Redact(errorResult, arcOptions.ExposeExceptionDetails, GetLogger(context));
        if (await TryWriteValidationRejection(context, errorResult, arcOptions))
        {
            return;
        }

        context.SetStatusCode((int)(UnauthorizedCause.IsCauseOf(ex) ? HttpStatusCode.Forbidden : HttpStatusCode.BadRequest));
        await context.WriteResponseAsJson(errorResult, typeof(QueryResult), context.RequestAborted);
    }

    static async Task<QueryResult> PerformQuery(IHttpRequestContext context, IQueryPerformer performer, QueryRequest request)
    {
        var queryPipeline = context.RequestServices.GetRequiredService<IQueryPipeline>();
        if (queryPipeline is QueryPipeline builtInPipeline)
        {
            return await builtInPipeline.PerformHosted(performer.FullyQualifiedName, request.Arguments, request.Paging, request.Sorting, context.RequestServices, context.RequestAborted);
        }

        if (!CanPerformWithCustomPipeline(context, performer, GetLogger(context)))
        {
            return QueryResult.Unauthorized(context.RequestServices.GetRequiredService<ICorrelationIdAccessor>().Current);
        }

        using var forwardedReceipt = OperationContextScope.ForwardTransportReceipt();
        return await queryPipeline.Perform(performer.FullyQualifiedName, request.Arguments, request.Paging, request.Sorting, context.RequestServices, context.RequestAborted);
    }

    static async Task ProcessQuery(IHttpRequestContext context, IQueryPerformer performer, QueryRequest request)
    {
        var observableQueryHandler = context.RequestServices.GetRequiredService<IObservableQueryHandler>();
        var arcOptions = context.RequestServices.GetRequiredService<IOptions<ArcOptions>>().Value;
        var nativeRequest = (context.RequestServices.GetService<IAuthorizationPolicyRuntime>() as IAuthorizationEmissionRuntime)?
            .CaptureLiveRequest(context.RequestServices);

        var queryResult = await PerformQuery(context, performer, request);
        using var ownedScope = queryResult.OwnedScope;

        // Check if the result data is a streaming result (Subject or AsyncEnumerable)
        if (queryResult.IsSuccess && observableQueryHandler.IsStreamingResult(queryResult.Data))
        {
            await StreamResult(context, performer, queryResult, nativeRequest);
            return;
        }

        await WriteQueryResponse(context, queryResult, arcOptions, GetLogger(context));
    }

    static async Task StreamResult(IHttpRequestContext context, IQueryPerformer performer, QueryResult queryResult, Func<ClaimsPrincipal, IServiceProvider, IDisposable?>? nativeRequest)
    {
        // The pipeline has restored the outer request identity. A direct stream lives beyond that lease,
        // so keep its subscriber identity, selected tenant and owned provider until the transport finishes.
        TenantId tenant;
        try
        {
            tenant = queryResult.AuthorizedTenant ?? context.RequestServices.GetRequiredService<TenantIdAccessor>().Current;
        }
        catch (Exception exception) when (UnauthorizedCause.IsCauseOf(exception))
        {
            context.SetStatusCode((int)HttpStatusCode.Forbidden);
            await context.WriteResponseAsJson(QueryResult.Unauthorized(queryResult.CorrelationId), typeof(QueryResult), context.RequestAborted);
            return;
        }

        var manager = context.RequestServices.GetRequiredService<IQueryContextManager>();
        var previousQueryContext = manager.Current;
        var queryContext = queryResult.AuthorizedQueryContext ?? previousQueryContext;
        if (queryContext != QueryContext.NotSet)
        {
            queryContext.EmissionTenant = tenant;
            queryContext.NativeEmissionRequest = nativeRequest;
            manager.Set(queryContext);
        }

        try
        {
            await HandleStreaming(context, performer, queryResult, new(tenant, nativeRequest));
        }
        finally
        {
            manager.Set(previousQueryContext);
        }
    }

    static async Task HandleStreaming(IHttpRequestContext context, IQueryPerformer performer, QueryResult queryResult, StreamEmission emission)
    {
        var handler = context.RequestServices.GetRequiredService<IObservableQueryHandler>();

        // No-scheme requests retain their original context, including any custom request implementation and
        // principal subclass. Only a selected identity needs a durable context with the owned B provider.
        if (queryResult.AuthorizedPrincipal is not { } principal)
        {
            await handler.HandleStreamingResult(context, performer.Name, queryResult.Data);
            return;
        }

        var selected = new ObservableQuerySubscriptionHttpRequestContext(
            context, context, queryResult.OwnedScope?.ServiceProvider ?? context.RequestServices, context.RequestAborted);
        selected.ConfigureEmission(emission.Tenant, emission.NativeRequest);
        selected.SelectAuthorizedPrincipal(principal, queryResult.OwnedScope?.ServiceProvider);
        await handler.HandleStreamingResult(selected, performer.Name, queryResult.Data);
    }

    static async Task WriteQueryResponse(IHttpRequestContext context, QueryResult queryResult, ArcOptions arcOptions, ILogger logger)
    {
        ExceptionDetailRedactor.Redact(queryResult, arcOptions.ExposeExceptionDetails, logger);
        if (await TryWriteValidationRejection(context, queryResult, arcOptions))
        {
            return;
        }

        var statusCode = EndpointRouteHelper.GetStatusCode(queryResult.IsSuccess, queryResult.IsAuthorized, queryResult.IsValid, queryResult.IsReady, queryResult.IsRetryable);
        context.SetStatusCode(statusCode);
        await context.WriteResponseAsJson(queryResult, typeof(QueryResult), context.RequestAborted);
    }

    static async Task<bool> TryWriteValidationRejection(IHttpRequestContext context, QueryResult result, ArcOptions options)
    {
        if (result.IsValid || !result.IsAuthorized)
        {
            return false;
        }

        var rejection = new QueryResult
        {
            CorrelationId = result.CorrelationId,
            ValidationResults = result.ValidationResults
        };

        if (context.WebSockets.IsWebSocketRequest)
        {
            using var socket = await context.WebSockets.AcceptWebSocket(context.RequestAborted);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(rejection, options.JsonSerializerOptions.ResolveTypeInfo<QueryResult>());
            await socket.Send(bytes, System.Net.WebSockets.WebSocketMessageType.Text, true, context.RequestAborted);
            await socket.Close(WebSocketCloseStatus.NormalClosure, "Query validation failed", context.RequestAborted);
            return true;
        }

        if (context.Headers.TryGetValue("Accept", out var accept) &&
            accept.Contains(HttpRequestContextExtensions.SseContentType, StringComparison.OrdinalIgnoreCase))
        {
            context.SetSseResponseHeaders();
            var json = JsonSerializer.Serialize(rejection, options.JsonSerializerOptions.ResolveTypeInfo<QueryResult>());
            await context.Write($"data: {json}\n\n", context.RequestAborted);
            return true;
        }

        return false;
    }

    static bool CanPerformWithCustomPipeline(IHttpRequestContext context, IQueryPerformer performer, ILogger logger)
    {
        try
        {
            var declarations = context.RequestServices.GetService<AuthorizationDeclarations>() ??
                throw new InvalidAuthorizationConfiguration("Authorization declarations are unavailable.");
            var target = QueryAuthorizationTarget.For(performer, declarations);
            var declaration = target is System.Reflection.MethodInfo method ? declarations.For(method) : declarations.For((Type)target);
            if (declaration.RequiresAsynchronousEvaluation)
            {
                throw new InvalidAuthorizationConfiguration($"Query '{performer.FullyQualifiedName}' requires an Arc pipeline that prepares authorization before execution.");
            }

            return true;
        }
        catch (Exception exception) when (exception is InvalidAuthorizationConfiguration or AmbiguousAuthorizationLevel)
        {
            logger.AuthorizationConfigurationFailed(exception);
            return false;
        }
    }

    static bool IsGet(IQueryRequestReader reader) => reader.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase);

    readonly record struct QueryEndpointPlacement(string Url, IEnumerable<string> LocationForTag, string? EndpointGroup);

    readonly record struct QueryEndpointTarget(IQueryRequestReader Reader, IQueryPerformer Performer, QueryEndpointPlacement Placement);

    readonly record struct StreamEmission(TenantId Tenant, Func<ClaimsPrincipal, IServiceProvider, IDisposable?>? NativeRequest);
}
