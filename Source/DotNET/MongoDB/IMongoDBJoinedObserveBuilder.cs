// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reactive.Subjects;

namespace Cratis.Arc.MongoDB;

#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Defines the builder for observing two joined collections.
/// </summary>
/// <typeparam name="T1">Type of the first (primary) document.</typeparam>
/// <typeparam name="T2">Type of the second (joined) document.</typeparam>
public interface IMongoDBJoinedObserveBuilder<T1, T2>
{
    /// <summary>
    /// Joins with another collection in the observation.
    /// </summary>
    /// <param name="filter">Optional filter expression for the joined documents.</param>
    /// <typeparam name="T3">Type of the third document.</typeparam>
    /// <returns>A <see cref="IMongoDBJoinedObserveBuilder{T1, T2, T3}"/> for further chaining or finalizing.</returns>
    IMongoDBJoinedObserveBuilder<T1, T2, T3> Join<T3>(
        Expression<Func<T3, bool>>? filter = null);

    /// <summary>
    /// Combines the observed collections into a reactive subject that emits a result whenever any collection changes.
    /// </summary>
    /// <param name="selector">A function that combines both collections into a single result value.</param>
    /// <typeparam name="TResult">Type of the combined result.</typeparam>
    /// <returns>An <see cref="ISubject{T}"/> that emits the combined result whenever any observed collection changes.</returns>
    ISubject<TResult> Select<TResult>(
        Func<IEnumerable<T1>, IEnumerable<T2>, TResult> selector);
}

/// <summary>
/// Defines the builder for observing three joined collections.
/// </summary>
/// <typeparam name="T1">Type of the first (primary) document.</typeparam>
/// <typeparam name="T2">Type of the second (joined) document.</typeparam>
/// <typeparam name="T3">Type of the third (joined) document.</typeparam>
public interface IMongoDBJoinedObserveBuilder<T1, T2, T3>
{
    /// <summary>
    /// Combines the observed collections into a reactive subject that emits a result whenever any collection changes.
    /// </summary>
    /// <param name="selector">A function that combines all three collections into a single result value.</param>
    /// <typeparam name="TResult">Type of the combined result.</typeparam>
    /// <returns>An <see cref="ISubject{T}"/> that emits the combined result whenever any observed collection changes.</returns>
    ISubject<TResult> Select<TResult>(
        Func<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, TResult> selector);
}
