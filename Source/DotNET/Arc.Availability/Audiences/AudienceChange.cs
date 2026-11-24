// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Audiences;

/// <summary>
/// Describes a change in the audiences granted at one level, so hosts can invalidate what they cached.
/// </summary>
/// <param name="Level">The level that changed.</param>
/// <param name="Key">The tenant or user whose audiences changed; <see langword="null"/> for the system level.</param>
public record AudienceChange(AudienceLevel Level, string? Key);
