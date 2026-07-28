// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents two policy requirements combined with a logical operator.
/// </summary>
/// <param name="Left">The left hand requirement.</param>
/// <param name="IsOr">Whether the requirements are combined with <c>or</c> rather than <c>and</c>.</param>
/// <param name="Right">The right hand requirement.</param>
/// <remarks>
/// Requirements registered one after the other all have to hold, while the several values one of them accepts are
/// alternatives. Build these trees left associatively - Screenplay has no parentheses in a policy condition, so
/// anything else changes meaning on a round trip.
/// </remarks>
public record CombinedRequirement(PolicyRequirementModel Left, bool IsOr, PolicyRequirementModel Right) : PolicyRequirementModel;
