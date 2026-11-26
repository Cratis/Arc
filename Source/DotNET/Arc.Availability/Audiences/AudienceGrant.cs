// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Audiences;

/// <summary>
/// An audience that is currently granted at one level, as an <see cref="IAudienceSource"/> reports it.
/// </summary>
/// <param name="Audience">The audience granted.</param>
/// <param name="Level">The level it is granted at.</param>
public record AudienceGrant(AudienceName Audience, AudienceLevel Level);
