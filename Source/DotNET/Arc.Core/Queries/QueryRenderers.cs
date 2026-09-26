// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Execution;
using Cratis.Types;

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
    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Query renderer types are discovered via ITypes.FindMultiple which preserves interfaces. Source-generated type discovery is the long-term fix (tracked in GitHub issue #2204).")]
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "queryProviderType.GetMethod returns a method from a framework-defined interface preserved by the type system. Source-generated dispatch is the long-term fix (tracked in GitHub issue #2204 item 3d).")]
    public QueryRendererResult Render(FullyQualifiedQueryName queryName, object query, IServiceProvider serviceProvider)
    {
        var queryType = query.GetType();
        var queryProviderType = _queryProviders.FirstOrDefault(_ => queryType.IsAssignableTo(_.GetInterface(typeof(IQueryRendererFor<>).Name)!.GetGenericArguments()[0]));
        var queryContext = queryContextManager.Current ?? new QueryContext(queryName, correlationIdAccessor.Current, Paging.NotPaged, Sorting.None);
        if (queryProviderType == null)
        {
            return new(queryContext.TotalItems, query);
        }

        var queryProvider = serviceProvider.GetService(queryProviderType);
        var method = queryProviderType.GetMethod(nameof(IQueryRendererFor<object>.Execute))!;
        return (method.Invoke(queryProvider, [query, queryContextManager.Current]) as QueryRendererResult)!;
    }
}