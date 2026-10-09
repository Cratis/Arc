// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Policies;

/// <summary>
/// Represents a claim matched against a proven artifact property or identifier.
/// </summary>
/// <param name="Claim">The claim type.</param>
/// <param name="Path">The authored artifact property path.</param>
/// <param name="MatchesSubject">Whether the path is the artifact's proven identifier.</param>
public record ClaimTargetRequirement(string Claim, string Path, bool MatchesSubject) : PolicyRequirementModel;
