// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Two conditions joined with <c>and</c> or <c>or</c>.
/// </summary>
/// <param name="Left">The first condition.</param>
/// <param name="IsAnd">Whether both must hold, or either may.</param>
/// <param name="Right">The second condition.</param>
public record LogicalConditionModel(AvailabilityConditionModel Left, bool IsAnd, AvailabilityConditionModel Right) : AvailabilityConditionModel;
