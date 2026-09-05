// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a policy requiring the caller to carry a claim with a given value.
/// </summary>
/// <param name="Claim">The name of the claim.</param>
/// <param name="Value">The value the claim has to match.</param>
public record ClaimRequirement(string Claim, string Value) : PolicyRequirementModel;
