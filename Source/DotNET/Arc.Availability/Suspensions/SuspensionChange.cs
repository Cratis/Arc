// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.Suspensions;

/// <summary>
/// A change to what is suspended.
/// </summary>
/// <param name="Level">The <see cref="AudienceLevel"/> that changed.</param>
/// <param name="Key">The tenant or user the change is for; null for the system level.</param>
public record SuspensionChange(AudienceLevel Level, string? Key);
