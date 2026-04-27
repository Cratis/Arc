// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Execution;
using Cratis.Strings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries;

/// <summary>
/// Maps query endpoints using the provided endpoint mapper.
/// </summary>
public static class QueryEndpointMapper
{
    const string SortByQueryStringKey = "sortby";
    const string SortDirectionQueryStringKey = "sortDirection";
    const string PageQueryStringKey = "page";
    const string PageSizeQueryStringKey = "pageSize";

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
            var location = performer.Location.Skip(options.SegmentsToSkipForRoute);
            var includeQueryName = EndpointRouteHelper.ShouldIncludeNameInRoute(
                options.IncludeQueryNameInRoute,
                location,
                performersByNamespace);
            var url = EndpointRouteHelper.BuildRouteUrl(options, location, performer.Name.ToString(), includeQueryName);

            if (!registeredUrls.Add(url)) continue;

            var executeEndpointName = $"Execute{performer.FullyQualifiedName}";
            if (!mapper.EndpointExists(executeEndpointName))
            {
                var metadata = new EndpointMetadata(
                    executeEndpointName,
                    $"Execute {performer.Name} query",
                    [string.Join('.', location)],
                    performer.AllowsAnonymousAccess,
                    ResponseType: typeof(QueryResult));

                mapper.MapGet(
                    url,
                    async context =>
                    {
                        var correlationIdAccessor = context.RequestServices.GetRequiredService<ICorrelationIdAccessor>();
                        var queryPipeline = context.RequestServices.GetRequiredService<IQueryPipeline>();
                        var observableQueryHandler = context.RequestServices.GetRequiredService<IObservableQueryHandler>();
                        var arcOptions = context.RequestServices.GetRequiredService<IOptions<ArcOptions>>().Value;

                        context.HandleCorrelationId(correlationIdAccessor, arcOptions.CorrelationId);

                        var paging = GetPagingInfo(context);
                        var sorting = GetSortingInfo(context);
                        var arguments = GetQueryArguments(context, performer);

                        var queryResult = await queryPipeline.Perform(performer.FullyQualifiedName, arguments, paging, sorting, context.RequestServices);

                        // Check if the result data is a streaming result (Subject or AsyncEnumerable)
                        if (queryResult.IsSuccess && observableQueryHandler.IsStreamingResult(queryResult.Data))
                        {
                            await observableQueryHandler.HandleStreamingResult(context, performer.Name, queryResult.Data);
                            return;
                        }

                        var statusCode = EndpointRouteHelper.GetStatusCode(queryResult.IsSuccess, queryResult.IsAuthorized, queryResult.IsValid);
                        context.SetStatusCode(statusCode);
                        await context.WriteResponseAsJson(queryResult, typeof(QueryResult), context.RequestAborted);
                    },
                    metadata);
            }
        }
    }

    static Paging GetPagingInfo(IHttpRequestContext context)
    {
        if (context.Query.TryGetValue(PageSizeQueryStringKey, out var pageSizeString) &&
            int.TryParse(pageSizeString, out var pageSize))
        {
            var page = 0;
            if (context.Query.TryGetValue(PageQueryStringKey, out var pageString) &&
                int.TryParse(pageString, out var parsedPage))
            {
                page = parsedPage;
            }

            return new Paging(page, pageSize, true);
        }

        return Paging.NotPaged;
    }

    static Sorting GetSortingInfo(IHttpRequestContext context)
    {
        if (context.Query.TryGetValue(SortByQueryStringKey, out var sortBy) &&
            context.Query.TryGetValue(SortDirectionQueryStringKey, out var sortDirection))
        {
            var sortByPascal = sortBy?.ToPascalCase();

            if (!string.IsNullOrEmpty(sortByPascal) && !string.IsNullOrEmpty(sortDirection))
            {
                var direction = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? SortDirection.Descending
                    : SortDirection.Ascending;
                return new Sorting(sortByPascal, direction);
            }
        }
        return Sorting.None;
    }

    static QueryArguments GetQueryArguments(IHttpRequestContext context, IQueryPerformer performer)
    {
        var arguments = new QueryArguments();

        var excludedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            SortByQueryStringKey,
            SortDirectionQueryStringKey,
            PageQueryStringKey,
            PageSizeQueryStringKey,
            ObservableQueryHttp.WaitForFirstResultQueryStringKey,
            ObservableQueryHttp.WaitForFirstResultTimeoutQueryStringKey
        };

        foreach (var kvp in context.Query)
        {
            if (!excludedKeys.Contains(kvp.Key) && !string.IsNullOrEmpty(kvp.Value))
            {
                var parameter = performer.Parameters.FirstOrDefault(p =>
                    string.Equals(p.Name, kvp.Key, StringComparison.OrdinalIgnoreCase));

                if (parameter is not null)
                {
                    var convertedValue = kvp.Value.ConvertTo(parameter.Type);
                    if (convertedValue is not null)
                    {
                        arguments[kvp.Key] = convertedValue;
                    }
                }
                else
                {
                    arguments[kvp.Key] = kvp.Value;
                }
            }
        }

        return arguments;
    }
}
