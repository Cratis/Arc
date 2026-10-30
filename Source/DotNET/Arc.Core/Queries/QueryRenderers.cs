// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an implementation of <see cref="IQueryRenderers"/>.
/// </summary>
/// <param name="queryContextManager"><see cref="IQueryContextManager"/> for managing query contexts.</param>
/// <param name="correlationIdAccessor"><see cref="ICorrelationIdAccessor"/> for getting the current correlation ID.</param>
/// <param name="types"><see cref="ITypes"/> for type discovery.</param>
public class QueryRenderers(
    IQueryContextManager queryContextManager,
    ICorrelationIdAccessor correlationIdAccessor,
    ITypes types) : IQueryRenderers
{
    readonly IEnumerable<Type> _queryProviders = types.FindMultiple(typeof(IQueryRendererFor<>));

    /// <inheritdoc/>
    public QueryRendererResult Render(FullyQualifiedQueryName queryName, object query, IServiceProvider serviceProvider)
    {
        var queryType = query.GetType();
        var queryProviderType = _queryProviders.FirstOrDefault(_ => queryType.IsAssignableTo(_.GetInterface(typeof(IQueryRendererFor<>).Name)!.GetGenericArguments()[0]));
        var queryContext = queryContextManager.Current ?? new QueryContext(queryName, correlationIdAccessor.Current, Paging.NotPaged, Sorting.None);
        if (queryProviderType == null)
        {
            return new(queryContext.TotalItems, query);
        }

        var queryProvider = (IQueryRenderer)serviceProvider.GetRequiredService(queryProviderType);
        return queryProvider.Execute(query, queryContextManager.Current);
    }
}