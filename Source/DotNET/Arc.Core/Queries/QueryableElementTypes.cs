// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Cratis.Arc.Queries;

/// <summary>
/// Registers the element types and sort key types of the <see cref="IQueryable"/> results of model-bound queries.
/// </summary>
/// <remarks>
/// Not intended to be called directly. Registering an element type makes counting and paging its queryables
/// statically typed, and registering a sort key type makes sorting on a property of that type statically typed, so
/// neither needs to be closed over the types at runtime. This registry does not imply that the rest of Arc supports
/// NativeAOT.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class QueryableElementTypes
{
    /// <summary>
    /// Registers an element type emitted by the Arc source generator.
    /// </summary>
    /// <typeparam name="TElement">The element type of a queryable query result.</typeparam>
    public static void Register<TElement>() =>
        QueryableExtensions.Register(typeof(TElement), new QueryableElementOperationsFor<TElement>());

    /// <summary>
    /// Registers a sort key type of an element type emitted by the Arc source generator.
    /// </summary>
    /// <typeparam name="TElement">The element type of a queryable query result.</typeparam>
    /// <typeparam name="TKey">The type of a property of the element type that the result can be sorted on.</typeparam>
    public static void RegisterSortKey<TElement, TKey>()
    {
        QueryableExtensions.RegisterSortKey(typeof(TElement), typeof(TKey), new QueryableSortKeyFor<TElement, TKey>());
        QueryableKeyTypes.Register<TKey>();
    }
}
