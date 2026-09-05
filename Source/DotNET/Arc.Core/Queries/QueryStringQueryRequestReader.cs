// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.DependencyInjection;
using Cratis.Strings;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an <see cref="IQueryRequestReader"/> that reads a query request from the URL query string of a GET request.
/// </summary>
[Singleton]
public class QueryStringQueryRequestReader : IQueryRequestReader
{
    const string SortByQueryStringKey = "sortby";
    const string SortDirectionQueryStringKey = "sortDirection";
    const string PageQueryStringKey = "page";
    const string PageSizeQueryStringKey = "pageSize";

    /// <inheritdoc/>
    public string HttpMethod => "GET";

    /// <inheritdoc/>
    public string EndpointNamePrefix => "Execute";

    /// <inheritdoc/>
    public Type? RequestBodyType => null;

    /// <inheritdoc/>
    public bool IncludeInApiDescription => true;

    /// <inheritdoc/>
    public string? ResponseCacheControl => null;

    /// <inheritdoc/>
    public Task<QueryRequest> Read(IHttpRequestContext context, IQueryPerformer performer) =>
        Task.FromResult(new QueryRequest(
            GetQueryArguments(context, performer),
            GetPagingInfo(context),
            GetSortingInfo(context)));

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
