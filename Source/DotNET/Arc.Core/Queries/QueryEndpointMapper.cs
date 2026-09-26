// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Cratis.Execution;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            .OrderByDescending(p => p.ReadModelType is { IsPublic: true } or { IsNestedPublic: true });
        var registeredUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var performer in orderedPerformers)
        {
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
                url = EndpointRouteHelper.BuildRouteUrl(options, performer.Location, options.SegmentsToSkipForRoute, performer.Name.ToString(), includeQueryName);
                locationForTag = location;
            }

            if (!registeredUrls.Add(url)) continue;

            foreach (var reader in readers)
            {
                MapForReader(mapper, reader, performer, url, locationForTag);
            }
        }
    }

    static void MapForReader(IEndpointMapper mapper, IQueryRequestReader reader, IQueryPerformer performer, string url, IEnumerable<string> locationForTag)
    {
        var endpointName = $"{reader.EndpointNamePrefix}{performer.FullyQualifiedName}";
        if (mapper.EndpointExists(endpointName))
        {
            return;
        }

        var metadata = new EndpointMetadata(
            endpointName,
            $"{reader.EndpointNamePrefix} {performer.Name} query",
            [string.Join('.', locationForTag)],
            performer.AllowsAnonymousAccess,
            RequestBodyType: reader.RequestBodyType,
            ResponseType: typeof(QueryResult),
            ExcludeFromApiDescription: !reader.IncludeInApiDescription);

        mapper.MapMethod(
            reader.HttpMethod,
            url,
            async context =>
            {
                using var receipt = OperationContextScope.Begin(context.RequestServices);
                var correlationIdAccessor = context.RequestServices.GetRequiredService<ICorrelationIdAccessor>();
                var arcOptions = context.RequestServices.GetRequiredService<IOptions<ArcOptions>>().Value;
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(QueryEndpointMapper).FullName!);

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
                    // A reader rejecting the request as invalid client input carries its own validation result, which
                    // is what names the reason on the wire. Everything else stays an exception result.
                    var errorResult = QueryResult.FromException(correlationIdAccessor.Current, ex);
                    ExceptionDetailRedactor.Redact(errorResult, arcOptions.ExposeExceptionDetails, logger);
                    context.SetStatusCode((int)HttpStatusCode.BadRequest);
                    await context.WriteResponseAsJson(errorResult, typeof(QueryResult), context.RequestAborted);
                    return;
                }

                await ProcessQuery(context, performer, request);
            },
            metadata);
    }

    static async Task ProcessQuery(IHttpRequestContext context, IQueryPerformer performer, QueryRequest request)
    {
        var queryPipeline = context.RequestServices.GetRequiredService<IQueryPipeline>();
        var observableQueryHandler = context.RequestServices.GetRequiredService<IObservableQueryHandler>();
        var arcOptions = context.RequestServices.GetRequiredService<IOptions<ArcOptions>>().Value;
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(QueryEndpointMapper).FullName!);
        var nativeRequest = (context.RequestServices.GetService<IAuthorizationPolicyRuntime>() as IAuthorizationEmissionRuntime)?
            .CaptureLiveRequest(context.RequestServices);

        QueryResult queryResult;
        if (queryPipeline is QueryPipeline builtInPipeline)
        {
            queryResult = await builtInPipeline.PerformHosted(performer.FullyQualifiedName, request.Arguments, request.Paging, request.Sorting, context.RequestServices, context.RequestAborted);
        }
        else
        {
            if (context.RequestServices.GetService<AuthorizationDeclarations>() is { } declarations)
            {
                var target = QueryAuthorizationTarget.For(performer, declarations);
                var declaration = target is System.Reflection.MethodInfo method ? declarations.For(method) : declarations.For((Type)target);
                if (declaration.RequiresAsynchronousEvaluation)
                {
                    throw new InvalidAuthorizationConfiguration($"Query '{performer.FullyQualifiedName}' requires an Arc pipeline that prepares authorization before execution.");
                }
            }

            queryResult = await queryPipeline.Perform(performer.FullyQualifiedName, request.Arguments, request.Paging, request.Sorting, context.RequestServices, context.RequestAborted);
        }

        using var ownedScope = queryResult.OwnedScope;

        // Check if the result data is a streaming result (Subject or AsyncEnumerable)
        if (queryResult.IsSuccess && observableQueryHandler.IsStreamingResult(queryResult.Data))
        {
            // The pipeline has restored the outer request identity. A direct stream lives beyond that lease,
            // so keep its subscriber identity, selected tenant and owned provider until the transport finishes.
            var tenant = queryResult.AuthorizedTenant ?? context.RequestServices.GetRequiredService<TenantIdAccessor>().Current;
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
                // No-scheme requests retain their original context, including any custom request implementation and
                // principal subclass. Only a selected identity needs a durable context with the owned B provider.
                if (queryResult.AuthorizedPrincipal is { } principal)
                {
                    var selected = new ObservableQuerySubscriptionHttpRequestContext(
                        context, context, queryResult.OwnedScope?.ServiceProvider ?? context.RequestServices, context.RequestAborted);
                    selected.ConfigureEmission(tenant, nativeRequest);
                    selected.SelectAuthorizedPrincipal(principal, queryResult.OwnedScope?.ServiceProvider);
                    await observableQueryHandler.HandleStreamingResult(selected, performer.Name, queryResult.Data);
                }
                else
                {
                    await observableQueryHandler.HandleStreamingResult(context, performer.Name, queryResult.Data);
                }
            }
            finally
            {
                manager.Set(previousQueryContext);
            }
            return;
        }

        ExceptionDetailRedactor.Redact(queryResult, arcOptions.ExposeExceptionDetails, logger);

        var statusCode = EndpointRouteHelper.GetStatusCode(queryResult.IsSuccess, queryResult.IsAuthorized, queryResult.IsValid, queryResult.IsReady);
        context.SetStatusCode(statusCode);
        await context.WriteResponseAsJson(queryResult, typeof(QueryResult), context.RequestAborted);
    }

    static bool IsGet(IQueryRequestReader reader) => reader.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase);
}
