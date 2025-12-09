// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Execution;
using Cratis.Strings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Http;

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

        var prefix = options.RoutePrefix.Trim('/');

        foreach (var performer in queryPerformerProviders.Performers)
        {
            var location = performer.Location.Skip(options.SegmentsToSkipForRoute);
            var segments = location.Select(segment => segment.ToKebabCase());
            var baseUrl = $"/{prefix}/{string.Join('/', segments)}";
            var typeName = options.IncludeQueryNameInRoute ? performer.Name.ToString() : string.Empty;

            var url = options.IncludeQueryNameInRoute ? $"{baseUrl}/{typeName.ToKebabCase()}" : baseUrl;
            url = url.ToLowerInvariant();

            var executeEndpointName = $"Execute{performer.Name}";
            if (!mapper.EndpointExists(executeEndpointName))
            {
                var metadata = new EndpointMetadata(
                    executeEndpointName,
                    $"Execute {performer.Name} query",
                    [string.Join('.', location)],
                    performer.AllowsAnonymousAccess);

                mapper.MapGet(
                    url,
                    async context =>
                    {
                        var correlationIdAccessor = context.RequestServices.GetRequiredService<ICorrelationIdAccessor>();
                        var queryPipeline = context.RequestServices.GetRequiredService<IQueryPipeline>();
                        var streamingQueryHandler = context.RequestServices.GetRequiredService<IObservableQueryHandler>();
                        var arcOptions = context.RequestServices.GetRequiredService<IOptions<ArcOptions>>().Value;

                        context.HandleCorrelationId(correlationIdAccessor, arcOptions.CorrelationId);

                        var paging = GetPagingInfo(context);
                        var sorting = GetSortingInfo(context);
                        var arguments = GetQueryArguments(context, performer);

                        var queryResult = await queryPipeline.Perform(performer.FullyQualifiedName, arguments, paging, sorting);

                        // Check if the result data is a streaming result (Subject or AsyncEnumerable)
                        if (queryResult.IsSuccess && streamingQueryHandler.IsStreamingResult(queryResult.Data))
                        {
                            await streamingQueryHandler.HandleStreamingResult(context, performer.Name, queryResult.Data);
                            return;
                        }

                        context.SetStatusCode(queryResult.IsSuccess ? 200 : !queryResult.IsValid ? 400 : 500);
                        await context.WriteResponseAsJsonAsync(queryResult, typeof(QueryResult), context.RequestAborted);
                    },
                    metadata);
            }
        }
    }

    static Paging GetPagingInfo(IHttpRequestContext context)
    {
        if (context.Query.TryGetValue(PageQueryStringKey, out var pageString) &&
            context.Query.TryGetValue(PageSizeQueryStringKey, out var pageSizeString) &&
            int.TryParse(pageString, out var page) &&
            int.TryParse(pageSizeString, out var pageSize))
        {
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
            PageSizeQueryStringKey
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
