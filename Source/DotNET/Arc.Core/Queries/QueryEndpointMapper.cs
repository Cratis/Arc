// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Http;
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
                    var errorResult = QueryResult.Error(correlationIdAccessor.Current, ex);
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

        var queryResult = await queryPipeline.Perform(performer.FullyQualifiedName, request.Arguments, request.Paging, request.Sorting, context.RequestServices, context.RequestAborted);

        // Check if the result data is a streaming result (Subject or AsyncEnumerable)
        if (queryResult.IsSuccess && observableQueryHandler.IsStreamingResult(queryResult.Data))
        {
            await observableQueryHandler.HandleStreamingResult(context, performer.Name, queryResult.Data);
            return;
        }

        ExceptionDetailRedactor.Redact(queryResult, arcOptions.ExposeExceptionDetails, logger);

        var statusCode = EndpointRouteHelper.GetStatusCode(queryResult.IsSuccess, queryResult.IsAuthorized, queryResult.IsValid, queryResult.IsReady);
        context.SetStatusCode(statusCode);
        await context.WriteResponseAsJson(queryResult, typeof(QueryResult), context.RequestAborted);
    }

    static bool IsGet(IQueryRequestReader reader) => reader.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase);
}
