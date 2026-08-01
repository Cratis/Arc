// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a constraint requiring an event to occur at most once per event source.
/// </summary>
/// <param name="Name">The name of the constraint.</param>
/// <param name="EventName">The name of the event the constraint applies to.</param>
public record UniqueEventConstraintModel(string Name, string EventName) : ConstraintModel(Name);
