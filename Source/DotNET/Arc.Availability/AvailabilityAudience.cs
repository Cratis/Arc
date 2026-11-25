// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents a declared audience.
/// </summary>
/// <param name="Name">The name of the audience.</param>
/// <param name="Description">What the audience stands for, or empty.</param>
/// <param name="Source">Where it is declared, or empty.</param>
public record AvailabilityAudience(string Name, string Description, string Source);
