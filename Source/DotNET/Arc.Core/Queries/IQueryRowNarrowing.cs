// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a system that narrows the rows a query returns, before sorting, paging and counting.
/// </summary>
/// <remarks>
/// A narrowing supplies a predicate over the read model, so a store-backed <see cref="IQueryable"/> filters in the store
/// and a page and its total count describe only the rows the caller may see. It is the row-level counterpart to
/// authorization, which admits or refuses the whole call. Register implementations as services.
/// </remarks>
public interface IQueryRowNarrowing
{
    /// <summary>
    /// Gets the predicate that keeps the rows the caller may see.
    /// </summary>
    /// <param name="readModelType">The read model type the query returns.</param>
    /// <param name="context">The <see cref="QueryContext"/> of the query being performed.</param>
    /// <returns>
    /// A lambda of the shape <c>Func&lt;TReadModel, bool&gt;</c>, or <see langword="null"/> when this narrowing does not apply
    /// to the query. An implementation that cannot decide must throw, so the rows are never returned unnarrowed.
    /// </returns>
    ValueTask<LambdaExpression?> GetPredicate(Type readModelType, QueryContext context);
}
