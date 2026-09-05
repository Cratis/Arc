// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a constraint requiring a property of an event to be unique.
/// </summary>
/// <param name="Name">The name of the constraint.</param>
/// <param name="Property">The property that has to be unique, in its original casing.</param>
/// <param name="EventName">The name of the event the constraint applies to.</param>
public record UniquePropertyConstraintModel(string Name, string Property, string EventName) : ConstraintModel(Name);
