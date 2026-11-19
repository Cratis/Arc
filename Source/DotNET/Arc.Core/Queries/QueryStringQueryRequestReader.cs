// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
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

    static readonly HashSet<string> _excludedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        SortByQueryStringKey,
        SortDirectionQueryStringKey,
        PageQueryStringKey,
        PageSizeQueryStringKey,
        ObservableQueryHttp.WaitForFirstResultQueryStringKey,
        ObservableQueryHttp.WaitForFirstResultTimeoutQueryStringKey
    };

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
        if (TryGetReservedKey(context.Query, PageSizeQueryStringKey, out var pageSizeString) &&
            int.TryParse(pageSizeString, out var pageSize))
        {
            var page = 0;
            if (TryGetReservedKey(context.Query, PageQueryStringKey, out var pageString) &&
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
        if (TryGetReservedKey(context.Query, SortByQueryStringKey, out var sortBy) &&
            TryGetReservedKey(context.Query, SortDirectionQueryStringKey, out var sortDirection))
        {
            var sortByPascal = sortBy?.ToPascalCase();

            if (!string.IsNullOrEmpty(sortByPascal) && !string.IsNullOrEmpty(sortDirection))
            {
                return new Sorting(sortByPascal, SortDirections.Parse(sortDirection, SortDirectionQueryStringKey));
            }
        }

        return Sorting.None;
    }

    static bool TryGetReservedKey(IReadOnlyDictionary<string, string> query, string key, out string? value)
    {
        if (query.TryGetValue(key, out value))
        {
            return true;
        }

        foreach (var item in query)
        {
            if (string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = item.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    static QueryArguments GetQueryArguments(IHttpRequestContext context, IQueryPerformer performer)
    {
        var arguments = new QueryArguments();

        foreach (var kvp in context.Query)
        {
            if (IsCandidateArgument(kvp))
            {
                AddQueryArgument(arguments, kvp, performer);
            }
        }

        return arguments;
    }

    static bool IsCandidateArgument(KeyValuePair<string, string> argument) =>
        !_excludedKeys.Contains(argument.Key) && !string.IsNullOrEmpty(argument.Value);

    static void AddQueryArgument(QueryArguments arguments, KeyValuePair<string, string> argument, IQueryPerformer performer)
    {
        var parameter = performer.Parameters.FirstOrDefault(p =>
            string.Equals(p.Name, argument.Key, StringComparison.OrdinalIgnoreCase));

        if (parameter is null)
        {
            arguments[argument.Key] = argument.Value;
            return;
        }

        if (IsJsonCollection(parameter.Type))
        {
            throw new InvalidQueryArgument(parameter.Name, parameter.Type, performer.FullyQualifiedName);
        }

        var convertedValue = argument.Value.ConvertQueryArgument(parameter.Type, parameter.Name, performer.FullyQualifiedName);

        if (convertedValue is not null)
        {
            arguments[argument.Key] = convertedValue;
        }
    }

    static bool IsJsonCollection(Type type)
    {
        if (!type.IsEnumerableOfQueryArgumentElement(out var elementType))
        {
            return false;
        }

        return elementType == typeof(JsonObject) || elementType == typeof(JsonArray);
    }
}
