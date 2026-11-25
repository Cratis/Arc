// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents a context value gates can read.
/// </summary>
/// <param name="Name">The name of the value.</param>
/// <param name="Type">The type of the value as authored.</param>
/// <param name="Origin">Where the value comes from.</param>
/// <param name="Required">Whether a gate that reads it is unknown when it is absent.</param>
public record AvailabilityContextValue(string Name, string Type, string Origin, bool Required);
