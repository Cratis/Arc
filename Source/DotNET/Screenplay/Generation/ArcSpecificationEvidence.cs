// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Generation;

/// <summary>
/// Creates exact source evidence and fail-closed adapter diagnostics.
/// </summary>
/// <param name="context">The analyzed application projects.</param>
/// <param name="scenarioProject">The scenario project.</param>
/// <param name="adapter">The adapter identity.</param>
/// <param name="diagnostics">The diagnostics to append.</param>
internal sealed class ArcSpecificationEvidence(
    DotNetAnalysisContext context,
    DotNetProjectCompilation scenarioProject,
    AdapterIdentity adapter,
    List<GenerationDiagnostic> diagnostics)
{
    /// <summary>
    /// Creates exact neutral evidence from an authored location.
    /// </summary>
    /// <param name="location">The exact authored location.</param>
    /// <param name="explanation">The evidence explanation.</param>
    /// <returns>The neutral evidence.</returns>
    public Evidence For(Location location, string? explanation = null)
    {
        var project = location.SourceTree is null ? scenarioProject : context.ProjectFor(location.SourceTree);
        return new()
        {
            Adapter = adapter,
            Strength = EvidenceStrength.Exact,
            Source = DotNetSource.RangeForProject(location, project),
            Explanation = explanation
        };
    }

    /// <summary>
    /// Reports one scenario that contributes no partial neutral facts.
    /// </summary>
    /// <param name="specification">The recovered legacy specification.</param>
    /// <param name="evidence">The exact scenario evidence.</param>
    /// <param name="reason">The blocking reason.</param>
    public void Block(SpecificationModel specification, SpecificationScenarioEvidence evidence, string reason) =>
        diagnostics.Add(new GenerationDiagnostic
        {
            Code = "ARCSP0001",
            Severity = GenerationDiagnosticSeverity.Warning,
            Outcome = GenerationDiagnosticOutcome.Unsupported,
            Message = $"Specification '{specification.Name}' contributed no neutral scenario because {reason}",
            Source = For(evidence.Source).Source,
            Subject = scenarioProject.SubjectForType(evidence.SourceType)
        });
}
