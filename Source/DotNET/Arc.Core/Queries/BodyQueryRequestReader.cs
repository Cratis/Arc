// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.DependencyInjection;
using Cratis.Strings;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an <see cref="IQueryRequestReader"/> that reads a query request from the JSON body of an
/// HTTP QUERY request (RFC 10008).
/// </summary>
[Singleton]
public class BodyQueryRequestReader : IQueryRequestReader
{
    /// <inheritdoc/>
    public string HttpMethod => "QUERY";

    /// <inheritdoc/>
    public string EndpointNamePrefix => "Query";

    /// <inheritdoc/>
    public Type? RequestBodyType => typeof(QueryRequestEnvelope);

    /// <inheritdoc/>
    public bool IncludeInApiDescription => false;

    /// <inheritdoc/>
    public string? ResponseCacheControl => "no-store";

    /// <inheritdoc/>
    public async Task<QueryRequest> Read(IHttpRequestContext context, IQueryPerformer performer)
    {
        var envelope = await context.ReadBodyAsJson(typeof(QueryRequestEnvelope), context.RequestAborted) as QueryRequestEnvelope
            ?? new QueryRequestEnvelope();

        return new QueryRequest(
            GetQueryArguments(envelope, performer),
            GetPagingInfo(envelope),
            GetSortingInfo(envelope));
    }

    static QueryArguments GetQueryArguments(QueryRequestEnvelope envelope, IQueryPerformer performer)
    {
        var arguments = new QueryArguments();
        if (envelope.Arguments is null)
        {
            return arguments;
        }

        foreach (var kvp in envelope.Arguments)
        {
            var rawValue = kvp.Value.ToString();
            if (string.IsNullOrEmpty(rawValue))
            {
                continue;
            }

            var parameter = performer.Parameters.FirstOrDefault(p =>
                string.Equals(p.Name, kvp.Key, StringComparison.OrdinalIgnoreCase));

            if (parameter is not null)
            {
                var convertedValue = rawValue.ConvertTo(parameter.Type);
                if (convertedValue is not null)
                {
                    arguments[kvp.Key] = convertedValue;
                }
            }
            else
            {
                arguments[kvp.Key] = rawValue;
            }
        }

        return arguments;
    }

    static Paging GetPagingInfo(QueryRequestEnvelope envelope) =>
        envelope.Paging is { PageSize: > 0 } paging
            ? new Paging(paging.Page, paging.PageSize, true)
            : Paging.NotPaged;

    static Sorting GetSortingInfo(QueryRequestEnvelope envelope)
    {
        if (envelope.Sorting is { } sorting && !string.IsNullOrEmpty(sorting.Field))
        {
            var sortByPascal = sorting.Field.ToPascalCase();
            var direction = sorting.Direction.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? SortDirection.Descending
                : SortDirection.Ascending;
            return new Sorting(sortByPascal, direction);
        }

        return Sorting.None;
    }
}
