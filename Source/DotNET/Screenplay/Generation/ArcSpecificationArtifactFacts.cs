// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
/// <param name="options">The host placement options.</param>
/// <param name="facts">The facts to append.</param>
internal sealed class ArcSpecificationArtifactFacts(
    DotNetAnalysisContext context,
    DotNetProjectCompilation scenarioProject,
    AdapterIdentity adapter,
    DotNetAdapterOptions options,
    List<GenerationFact> facts)
{
    /// <summary>
    /// Adds or resolves one exact source artifact.
    /// </summary>
    /// <param name="type">The exact source type.</param>
    /// <param name="kind">The neutral artifact kind.</param>
    /// <param name="source">The referencing step source.</param>
    /// <returns>The artifact key, or <see langword="null"/> when source identity is ambiguous.</returns>
    public ArtifactKey? Artifact(INamedTypeSymbol type, ArtifactKind kind, Location source)
    {
        var subject = context.SubjectForType(type);
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
                    Properties = [.. type.GetMembers().OfType<IPropertySymbol>()
                        .Where(property => !property.IsStatic)
                        .OrderBy(property => property.Name, StringComparer.Ordinal)
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
    /// Adds the current compatibility placement for the exact scenario target.
    /// </summary>
    /// <param name="target">The exact target artifact.</param>
    /// <param name="type">The exact source type.</param>
    /// <param name="source">The source evidence.</param>
    public void AddPlacement(ArtifactKey target, INamedTypeSymbol type, Location source)
    {
        var segments = type.ContainingNamespace.ToDisplayString().Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Skip(options.NamespaceSegmentsToSkip)
            .ToArray();
        if (segments.Length == 0)
        {
            return;
        }

        var module = options.Module ?? segments[0];
        var remaining = segments.Skip(string.Equals(segments[0], module, StringComparison.Ordinal) ? 1 : 0).ToArray();
        var slice = remaining.Length == 0 ? type.Name : remaining[^1];
        var features = remaining.Length <= 1 ? [] : remaining[..^1];
        facts.Add(new ArtifactPlacementFact
        {
            Id = FactId("placement", target.Subject, target.Kind.ToString()),
            Subject = target.Subject,
            Evidence = Evidence(source, "The target namespace places the specification under its exact owning slice"),
            Artifact = target,
            Placement = new ArtifactPlacement
            {
                Module = module,
                Features = features,
                Slice = slice,
                SliceKind = GenerationSliceKind.StateChange
            }
        });
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
