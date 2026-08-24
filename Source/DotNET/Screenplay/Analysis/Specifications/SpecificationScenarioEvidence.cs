// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Represents exact scenario, step, value, rejection, and atomic-admission evidence.
/// </summary>
/// <param name="SourceType">The exact authored scenario type.</param>
/// <param name="Source">The exact authored scenario declaration.</param>
/// <param name="States">State steps keyed by their legacy model instances using reference identity.</param>
/// <param name="Values">Values keyed by their legacy model instances using reference identity.</param>
/// <param name="Errors">Rejection assertion locations in legacy model order.</param>
/// <param name="Blockers">Diagnostics proving neutral fact contribution must fail atomically.</param>
internal sealed record SpecificationScenarioEvidence(
    INamedTypeSymbol SourceType,
    Location Source,
    IReadOnlyDictionary<SpecificationStateModel, SpecificationStateEvidence> States,
    IReadOnlyDictionary<PropertyMappingModel, Location> Values,
    IReadOnlyList<Location> Errors,
    IReadOnlyList<ScreenplayDiagnostic> Blockers);
