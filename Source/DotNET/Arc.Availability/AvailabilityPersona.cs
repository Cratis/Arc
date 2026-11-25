// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents a named set of audiences.
/// </summary>
/// <param name="Name">The name of the persona.</param>
/// <param name="Audiences">The audiences the persona belongs to.</param>
public record AvailabilityPersona(string Name, IReadOnlyList<string> Audiences);
