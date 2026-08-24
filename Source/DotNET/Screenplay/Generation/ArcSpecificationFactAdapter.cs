// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Generation;

/// <summary>
/// Contributes exact Arc specification source evidence as neutral Generation facts.
/// </summary>
public sealed class ArcSpecificationFactAdapter : IDotNetScreenplayAdapter
{
    const string AdapterId = "cratis.arc.specifications";
    const string AdapterVersion = "1.0.0";

    /// <inheritdoc/>
    public AdapterIdentity Identity { get; } = new() { Id = AdapterId, Version = AdapterVersion };

    /// <inheritdoc/>
    public bool CanAnalyze(DotNetAnalysisContext context) =>
        context.Projects.SelectMany(project => ArtifactCatalog.From(project.Compilation).Types)
            .Any(type => SpecificationMembers.CommandOf(type) is not null || SpecificationMembers.ReadModelOf(type) is not null);

    /// <inheritdoc/>
    public AdapterContribution Analyze(DotNetAnalysisContext context, DotNetAdapterOptions options)
    {
        var facts = new List<GenerationFact>();
        var diagnostics = new List<GenerationDiagnostic>();
        var models = new SemanticModels([.. context.Projects.Select(project => project.Compilation)]);
        var readerDiagnostics = new ScreenplayDiagnostics();
        var reader = new SpecificationReader(models, readerDiagnostics);

        foreach (var project in context.Projects)
        {
            foreach (var type in ArtifactCatalog.From(project.Compilation).Types.Where(reader.IsSpecification))
            {
                var target = SpecificationMembers.CommandOf(type) ?? SpecificationMembers.ReadModelOf(type);
                if (target is not INamedTypeSymbol namedTarget)
                {
                    continue;
                }

                var slice = namedTarget.ContainingNamespace.ToDisplayString();
                var name = SpecificationPlacement.NameOf(type, slice);
                var diagnosticCount = readerDiagnostics.All.Count;
                if (reader.Read(type, name) is not { } specification)
                {
                    var reason = string.Join("; ", readerDiagnostics.All.Skip(diagnosticCount).Select(item => item.Message));
                    diagnostics.Add(Unreadable(project, type, reason));
                    continue;
                }

                if (SpecificationEvidence.For(specification) is not { } evidence)
                {
                    diagnostics.Add(Unreadable(project, type, "source evidence was not retained"));
                    continue;
                }

                new ArcSpecificationFactBuilder(context, project, Identity, options, facts, diagnostics)
                    .Add(specification, evidence, namedTarget);
            }
        }

        return new()
        {
            Adapter = Identity,
            Facts = facts,
            Diagnostics = diagnostics
        };
    }

    GenerationDiagnostic Unreadable(DotNetProjectCompilation project, INamedTypeSymbol type, string reason) => new()
    {
        Code = "ARCSP0001",
        Severity = GenerationDiagnosticSeverity.Warning,
        Outcome = GenerationDiagnosticOutcome.Unsupported,
        Message = $"Specification '{type.Name}' contributed no neutral scenario because {reason}",
        Source = DotNetSource.EvidenceFor(type, Identity, project, EvidenceStrength.Exact).Source,
        Subject = project.SubjectForType(type)
    };
}
