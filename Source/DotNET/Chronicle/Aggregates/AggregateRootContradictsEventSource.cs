// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Aggregates;

/// <summary>
/// Exception that gets thrown when an aggregate root's event source declaration contradicts its other routing.
/// </summary>
/// <param name="aggregateRootType">The type of the aggregate root.</param>
/// <param name="dimension">The routing dimension that contradicts.</param>
/// <param name="expected">The value the event source declaration resolves to.</param>
/// <param name="actual">The contradicting value.</param>
public class AggregateRootContradictsEventSource(Type aggregateRootType, string dimension, string expected, string actual)
    : Exception($"Aggregate root '{aggregateRootType.FullName ?? aggregateRootType.Name}' declares {dimension} '{actual}', which contradicts event source routing '{expected}'.");
