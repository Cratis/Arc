// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Cratis.Arc.Queries;

/// <summary>
/// Registers the element types of observable queries, using the registrations the Arc source generator emits for the
/// <see cref="System.Reactive.Subjects.ISubject{T}"/> and <see cref="IAsyncEnumerable{T}"/> results of model-bound queries.
/// </summary>
/// <remarks>
/// Not intended to be called directly. Registering an element type makes its observable query operations statically
/// typed, so they do not need to be closed over the element type at runtime. This registry does not imply that the
/// rest of Arc supports NativeAOT.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ObservableQueryElementTypes
{
    /// <summary>
    /// Registers an element type emitted by the Arc source generator.
    /// </summary>
    /// <typeparam name="TElement">The element type of an observable query result.</typeparam>
    public static void Register<TElement>() =>
        ObservableQueryElementOperations.Register(typeof(TElement), new ObservableQueryElementOperationsFor<TElement>());
}
