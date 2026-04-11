// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Defines the builder for observing a single collection, enabling joins with other collections.
/// </summary>
/// <typeparam name="TDocument">Type of the primary document.</typeparam>
public interface IMongoDBObserveBuilder<TDocument>
{
    /// <summary>
    /// Joins with another collection in the observation.
    /// </summary>
    /// <param name="filter">Optional filter expression for the joined documents.</param>
    /// <typeparam name="TJoined">Type of the joined document.</typeparam>
    /// <returns>A <see cref="IMongoDBJoinedObserveBuilder{T1, T2}"/> for further chaining or finalizing.</returns>
    IMongoDBJoinedObserveBuilder<TDocument, TJoined> Join<TJoined>(
        Expression<Func<TJoined, bool>>? filter = null);
}
