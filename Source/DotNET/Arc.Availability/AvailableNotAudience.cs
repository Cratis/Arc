// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability;

/// <summary>
/// 'not' over one audience: holds when the audience is not held.
/// </summary>
/// <remarks>
/// The logic is three-valued. When the audiences cannot be decided the result is unknown, which never opens a gate.
/// </remarks>
/// <param name="Audience">The audience that must not be held.</param>
public record AvailableNotAudience(AudienceName Audience) : AvailabilityExpression;
