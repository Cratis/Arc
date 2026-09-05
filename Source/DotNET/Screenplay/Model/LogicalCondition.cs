// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents two conditions combined with a logical operator.
/// </summary>
/// <param name="Left">The left hand condition.</param>
/// <param name="IsOr">Whether the conditions are combined with <c>or</c> rather than <c>and</c>.</param>
/// <param name="Right">The right hand condition.</param>
/// <remarks>
/// Screenplay has no parentheses in conditions, so nesting is flattened when printed. Build these trees left
/// associatively - anything else changes meaning on a round trip.
/// </remarks>
public record LogicalCondition(ConditionModel Left, bool IsOr, ConditionModel Right) : ConditionModel;
