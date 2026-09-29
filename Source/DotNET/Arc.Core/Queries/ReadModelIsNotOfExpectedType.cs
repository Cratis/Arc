// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Exception that gets thrown when an item passed to read model interceptors is not of the read model type they intercept.
/// </summary>
/// <param name="actualType">The type of the item.</param>
/// <param name="expectedType">The read model type the interceptors intercept.</param>
internal sealed class ReadModelIsNotOfExpectedType(Type actualType, Type expectedType)
    : ArgumentException($"Object of type '{actualType}' cannot be converted to type '{expectedType}'.");
