// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Queries;
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
    public bool CanAnalyze(DotNetAnalysisContext context)
    {
        var types = context.Projects.SelectMany(project => ArtifactCatalog.From(project.Compilation).Types).ToArray();
        var hasLegacySpecification = types.Any(type =>
            SpecificationMembers.CommandOf(SpecificationMembers.StepsOf(type)) is not null ||
            SpecificationMembers.ReadModelOf(SpecificationMembers.StepsOf(type)) is not null);
        var hasQuerySpecification = SpecificationQueryCatalog.From(context).Count > 0 && types.Any(IsQuerySpecificationShape);
        return hasLegacySpecification || hasQuerySpecification;
    }

    /// <inheritdoc/>
    public AdapterContribution Analyze(DotNetAnalysisContext context, DotNetAdapterOptions options)
    {
        var facts = new List<GenerationFact>();
        var diagnostics = new List<GenerationDiagnostic>();
        var candidates = new List<ArcSpecificationFactCandidate>();
        var sourceStructures = DotNetSourceStructures.Create(context);
        diagnostics.AddRange(sourceStructures.Diagnostics);
        var models = new SemanticModels([.. context.Projects.Select(project => project.Compilation)]);
        var readerDiagnostics = new ScreenplayDiagnostics();
        var reader = new SpecificationReader(models, readerDiagnostics);
        var queryReader = new SpecificationQueryReader(models, SpecificationQueryCatalog.From(context));

        foreach (var project in context.Projects)
        {
            foreach (var type in ArtifactCatalog.From(project.Compilation).Types)
            {
                if (reader.IsSpecification(type))
                {
                    var steps = SpecificationMembers.StepsOf(type);
                    var target = SpecificationMembers.CommandOf(steps) ?? SpecificationMembers.ReadModelOf(steps);
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

                    var candidate = new ArcSpecificationFactBuilder(context, project, Identity, options, sourceStructures, diagnostics)
                        .Build(specification, evidence, namedTarget);
                    if (candidate is not null)
                    {
                        candidates.Add(candidate);
                    }

                    continue;
                }

                if (queryReader.TryRead(type, out var queryEvidence, out var queryReason))
                {
                    var queryName = SpecificationPlacement.NameOf(type, queryEvidence!.ReadModel.ContainingNamespace.ToDisplayString());
                    var queryCandidate = new ArcSpecificationQueryFactBuilder(context, project, Identity, options, sourceStructures, diagnostics)
                        .Build(queryName, queryEvidence);
                    if (queryCandidate is not null)
                    {
                        candidates.Add(queryCandidate);
                    }

                    continue;
                }

                if (queryReason is not null)
                {
                    diagnostics.Add(Unreadable(project, type, queryReason));
                }
            }
        }

        var placements = DotNetSourcePlacementDerivation.Derive(candidates.SelectMany(_ => _.PlacementRequests));
        diagnostics.AddRange(placements.Diagnostics);
        foreach (var candidate in candidates)
        {
            var candidatePlacements = candidate.PlacementRequests
                .Select(request => placements.Placements.SingleOrDefault(_ => _.Artifact == request.Artifact))
                .ToArray();
            if (candidatePlacements.Any(_ => _ is null))
            {
                continue;
            }

            foreach (var fact in candidate.Facts)
            {
                if (!facts.Exists(_ => _.Id == fact.Id))
                {
                    facts.Add(fact);
                }
            }

            foreach (var placement in candidatePlacements.OfType<DotNetSourcePlacement>())
            {
                var placementFact = ArcSpecificationArtifactFacts.Placement(Identity, placement);
                if (!facts.Exists(_ => _.Id == placementFact.Id))
                {
                    facts.Add(placementFact);
                }
            }
        }

        return new()
        {
            Adapter = Identity,
            Facts = facts,
            Diagnostics = diagnostics
        };
    }

    static bool IsQuerySpecificationShape(INamedTypeSymbol type) =>
        type is { TypeKind: TypeKind.Class, IsAbstract: false, ContainingType: null } &&
        SpecificationMembers.MethodsIn(SpecificationMembers.StepsOf(type), SpecificationMembers.BecauseMethod).Any() &&
        SpecificationMembers.AssertionsIn(type).Any();

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
