// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Strings;
using Microsoft.AspNetCore.Http;

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
    /// Extracts paging information from the HTTP request query string.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>The paging information.</returns>
    public static Paging GetPagingInfo(this HttpContext httpContext)
    {
        if (httpContext.Request.Query.ContainsKey(PageQueryStringKey) &&
            httpContext.Request.Query.ContainsKey(PageSizeQueryStringKey) &&
            int.TryParse(httpContext.Request.Query[PageQueryStringKey].ToString(), out var page) &&
            int.TryParse(httpContext.Request.Query[PageSizeQueryStringKey].ToString(), out var pageSize))
        {
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
    public static QueryArguments GetQueryArguments(this HttpContext httpContext, IQueryPerformer performer)
    {
        var arguments = new QueryArguments();

        var excludedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            SortByQueryStringKey,
            SortDirectionQueryStringKey,
            PageQueryStringKey,
            PageSizeQueryStringKey
        };

        foreach (var kvp in httpContext.Request.Query)
        {
            if (!excludedKeys.Contains(kvp.Key) && !string.IsNullOrEmpty(kvp.Value))
            {
                var stringValue = kvp.Value.ToString();

                var parameter = performer.Parameters.FirstOrDefault(p =>
                    string.Equals(p.Name, kvp.Key, StringComparison.OrdinalIgnoreCase));

                if (parameter is not null)
                {
                    var convertedValue = stringValue.ConvertTo(parameter.Type);
                    if (convertedValue is not null)
                    {
                        arguments[kvp.Key] = convertedValue;
                    }
                }
                else
                {
                    arguments[kvp.Key] = stringValue;
                }
            }
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
        if (!queryResult.IsAuthorized)
        {
            response.StatusCode = (int)HttpStatusCode.Forbidden;
        }
        else if (!queryResult.IsValid)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
        else if (queryResult.HasExceptions)
        {
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }

        // else 200 OK (default)
    }
}
