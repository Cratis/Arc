// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;

using static Cratis.Arc.Screenplay.Generation.ArcSpecificationFacts;

namespace Cratis.Arc.Screenplay.Generation;

/// <summary>
/// Contributes the exact artifacts and target placement required by an Arc specification scenario.
/// </summary>
/// <param name="context">The analyzed application projects.</param>
/// <param name="scenarioProject">The project declaring the scenario.</param>
/// <param name="adapter">The adapter identity.</param>
/// <param name="sourceStructures">The fixed source-structure snapshot.</param>
/// <param name="facts">The facts to append.</param>
internal sealed class ArcSpecificationArtifactFacts(
    DotNetAnalysisContext context,
    DotNetProjectCompilation scenarioProject,
    AdapterIdentity adapter,
    DotNetSourceStructureSnapshot sourceStructures,
    List<GenerationFact> facts)
{
    /// <summary>
    /// Creates the neutral placement fact from one exact shared placement.
    /// </summary>
    /// <param name="adapter">The contributing adapter.</param>
    /// <param name="placement">The shared placement.</param>
    /// <returns>The placement fact.</returns>
    public static ArtifactPlacementFact Placement(AdapterIdentity adapter, DotNetSourcePlacement placement) => new()
    {
        Id = FactId("placement", placement.Artifact.Subject, placement.Artifact.Kind.ToString()),
        Subject = placement.Artifact.Subject,
        Evidence = new()
        {
            Adapter = adapter,
            Strength = EvidenceStrength.Exact,
            Source = placement.Structure.Source,
            Explanation = "The shared source-placement derivation places the exact scenario artifact"
        },
        Artifact = placement.Artifact,
        Placement = placement.Placement
    };

    /// <summary>
    /// Adds or resolves one exact source artifact.
    /// </summary>
    /// <param name="type">The exact source type.</param>
    /// <param name="kind">The neutral artifact kind.</param>
    /// <param name="source">The referencing step source.</param>
    /// <returns>The artifact key, or <see langword="null"/> when source identity is ambiguous.</returns>
    public ArtifactKey? Artifact(INamedTypeSymbol type, ArtifactKind kind, Location source)
    {
        var sourceProjects = context.Projects
            .Where(project => type.DeclaringSyntaxReferences.Any(_ => project.AuthoredSyntaxTrees.Contains(_.SyntaxTree)))
            .ToArray();
        var subject = sourceProjects.Length == 1
            ? sourceProjects[0].SubjectForType(type)
            : context.SubjectForType(type);
        if (subject is null)
        {
            return null;
        }

        var key = new ArtifactKey { Subject = subject, Kind = kind };
        if (!facts.OfType<ArtifactFact>().Any(fact => fact.Definition.Key == key))
        {
            var evidence = Evidence(source, "The exact specification step references this source artifact");
            facts.Add(new ArtifactFact
            {
                Id = FactId("artifact", subject, kind.ToString()),
                Subject = subject,
                Evidence = evidence,
                Definition = new ArtifactDefinition
                {
                    Key = key,
                    Name = type.Name,
                    File = evidence.Source?.Path,
                    Properties = [.. type.DeclaredProperties()
                        .Select(property => new PropertyDefinition
                        {
                            Name = property.Name,
                            Type = TypeReference(property.Type, context)
                        })]
                }
            });
        }

        return key;
    }

    /// <summary>
    /// Creates a shared placement request for one exact scenario artifact.
    /// </summary>
    /// <param name="target">The exact scenario artifact.</param>
    /// <param name="sliceKind">The independently established semantic slice kind.</param>
    /// <param name="policy">The host-owned source-structure policy.</param>
    /// <returns>The placement request, or <see langword="null"/> when no exact source structure exists.</returns>
    public DotNetSourcePlacementRequest? PlacementRequest(
        ArtifactKey target,
        GenerationSliceKind sliceKind,
        DotNetSourceStructurePolicy policy)
    {
        var structure = sourceStructures.Structures.SingleOrDefault(_ => _.Subject == target.Subject);
        return structure is null
            ? null
            : new()
            {
                Artifact = target,
                Structure = structure,
                SliceKind = sliceKind,
                Policy = policy
            };
    }

    Evidence Evidence(Location location, string? explanation = null)
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
}
