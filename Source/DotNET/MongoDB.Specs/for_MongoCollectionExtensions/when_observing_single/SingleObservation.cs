// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing_single;

/// <summary>
/// The single-result observation overloads a spec exercises.
/// </summary>
public enum SingleObservation
{
    /// <summary>
    /// <c>ObserveSingle</c> with an expression.
    /// </summary>
    Expression = 0,

    /// <summary>
    /// <c>ObserveSingle</c> with a filter definition.
    /// </summary>
    Filter = 1,

    /// <summary>
    /// <c>ObserveById</c>.
    /// </summary>
    ById = 2,

    /// <summary>
    /// <c>ObserveSingle</c> with an expression and <c>ignoreQueryContext: false</c>.
    /// </summary>
    ExpressionWithContext = 3,

    /// <summary>
    /// <c>ObserveSingle</c> with a filter definition and <c>ignoreQueryContext: false</c>.
    /// </summary>
    FilterWithContext = 4,

    /// <summary>
    /// <c>ObserveById</c> with <c>ignoreQueryContext: false</c>.
    /// </summary>
    ByIdWithContext = 5
}
