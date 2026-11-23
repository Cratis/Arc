// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Exceptions;
using Cratis.Arc.Http;
using Cratis.Strings;

namespace Cratis.Arc.Queries;

/// <summary>
/// Provides common query processing functionality that can be used by both controller-based queries and pipeline-based queries.
/// </summary>
public static class QueryHttpExtensions
{
    /// <summary>
    /// Gets the key for the sort by query string.
    /// </summary>
    public const string SortByQueryStringKey = "sortby";

    /// <summary>
    /// Gets the key for the sort direction query string.
    /// </summary>
    public const string SortDirectionQueryStringKey = "sortDirection";

    /// <summary>
    /// Gets the key for the page query string.
    /// </summary>
    public const string PageQueryStringKey = "page";

    /// <summary>
    /// Gets the key for the page size query string.
    /// </summary>
    public const string PageSizeQueryStringKey = "pageSize";

    /// <summary>
    /// Gets the observable query HTTP options from the HTTP request query string.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>The <see cref="ObservableQueryHttpOptions"/>.</returns>
    public static ObservableQueryHttpOptions GetObservableQueryHttpOptions(this HttpContext httpContext) =>
        ObservableQueryHttp.GetOptions(httpContext.Request.Query.ToDictionary(_ => _.Key, _ => _.Value.ToString(), StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Extracts paging information from the HTTP request query string.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>The paging information.</returns>
    public static Paging GetPagingInfo(this HttpContext httpContext)
    {
        if (httpContext.Request.Query.ContainsKey(PageSizeQueryStringKey) &&
            int.TryParse(httpContext.Request.Query[PageSizeQueryStringKey].ToString(), out var pageSize))
        {
            var page = 0;
            if (httpContext.Request.Query.ContainsKey(PageQueryStringKey) &&
                int.TryParse(httpContext.Request.Query[PageQueryStringKey].ToString(), out var parsedPage))
            {
                page = parsedPage;
            }

            return new Paging(page, pageSize, true);
        }

        return Paging.NotPaged;
    }

    /// <summary>
    /// Extracts sorting information from the HTTP request query string.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>The sorting information.</returns>
    public static Sorting GetSortingInfo(this HttpContext httpContext)
    {
        if (httpContext.Request.Query.ContainsKey(SortByQueryStringKey) &&
            httpContext.Request.Query.ContainsKey(SortDirectionQueryStringKey))
        {
            var sortBy = httpContext.Request.Query[SortByQueryStringKey].ToString()?.ToPascalCase();
            var sortDirection = httpContext.Request.Query[SortDirectionQueryStringKey].ToString();

            if (!string.IsNullOrEmpty(sortBy) && !string.IsNullOrEmpty(sortDirection))
            {
                var direction = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? SortDirection.Descending
                    : SortDirection.Ascending;
                return new Sorting(sortBy, direction);
            }
        }
        return Sorting.None;
    }

    /// <summary>
    /// Extracts custom parameters from the HTTP request query string, excluding standard query processing parameters.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="performer">The query performer to get for.</param>
    /// <returns>A dictionary of custom parameters.</returns>
    /// <exception cref="InvalidQueryArgument">A supplied scalar argument cannot be converted.</exception>
    public static QueryArguments GetQueryArguments(this HttpContext httpContext, IQueryPerformer performer)
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

        foreach (var kvp in httpContext.Request.Query)
        {
            if (excludedKeys.Contains(kvp.Key) || string.IsNullOrEmpty(kvp.Value)) continue;
            var stringValue = kvp.Value.ToString();
            var parameter = performer.Parameters.FirstOrDefault(p =>
                string.Equals(p.Name, kvp.Key, StringComparison.OrdinalIgnoreCase));
            if (parameter is null)
            {
                arguments[kvp.Key] = stringValue;
                continue;
            }
            var convertedValue = stringValue.ConvertQueryArgument(parameter.Type, parameter.Name, performer.FullyQualifiedName);
            if (convertedValue is not null) arguments[kvp.Key] = convertedValue;
        }

        return arguments;
    }

    /// <summary>
    /// Sets the HTTP response status code based on the query result.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="queryResult">The query result.</param>
    public static void SetResponseStatusCode(this HttpResponse response, QueryResult queryResult)
    {
        var mapping = ExceptionResponseMapping.ResponseFor(queryResult);
        if (mapping is not null && response.HasStarted) return;
        if (HasHttpFailure(queryResult))
        {
            response.StatusCode = (int)(mapping?.StatusCode ?? EndpointRouteHelper.GetStatusCode(false, queryResult.IsAuthorized, queryResult.IsValid, isRetryable: queryResult.IsRetryable));
        }
        if (mapping is not null)
        {
            foreach (var header in mapping.Headers) response.Headers[header.Key] = header.Value;
        }

        // else 200 OK (default)
    }

    static bool HasHttpFailure(QueryResult result) => !result.IsAuthorized || !result.IsValid || result.IsRetryable || result.HasExceptions;
}
