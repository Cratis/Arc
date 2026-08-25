// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Holds neutral-fact source evidence without changing legacy model equality or public API shape.
/// </summary>
internal static class SpecificationEvidence
{
    static readonly ConditionalWeakTable<SpecificationModel, SpecificationScenarioEvidence> _evidence = new();

    /// <summary>
    /// Registers source evidence for one recovered specification model.
    /// </summary>
    /// <param name="specification">The recovered specification.</param>
    /// <param name="evidence">The exact source evidence.</param>
    public static void Register(SpecificationModel specification, SpecificationScenarioEvidence evidence) =>
        _evidence.Add(specification, evidence);

    /// <summary>
    /// Gets source evidence for one recovered specification when it was produced by source analysis.
    /// </summary>
    /// <param name="specification">The recovered specification.</param>
    /// <returns>The evidence, or <see langword="null"/> when no source analysis registered it.</returns>
    public static SpecificationScenarioEvidence? For(SpecificationModel specification) =>
        _evidence.TryGetValue(specification, out var evidence) ? evidence : null;
}
