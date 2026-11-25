// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Reads the availability the code declares as types: audiences, availability blocks, scope markers and provided read models.
/// </summary>
public static class AvailabilityDeclarations
{
    /// <summary>
    /// Reads the declarations of the compilations.
    /// </summary>
    /// <param name="compilations">The compilations to read.</param>
    /// <param name="catalogs">What each compilation declares, in the order of the compilations.</param>
    /// <param name="diagnostics">Where what cannot be stated is reported.</param>
    /// <returns>The audiences, the blocks, the gates of scope markers and the provided values.</returns>
    public static (IReadOnlyList<AudienceModel> Audiences, IReadOnlyList<AvailabilityBlockModel> Blocks, IReadOnlyList<ScopeGateModel> ScopeGates, IReadOnlyList<ContextValueModel> Provided) In(
        IReadOnlyList<Compilation> compilations,
        IReadOnlyList<ArtifactCatalog> catalogs,
        ScreenplayDiagnostics diagnostics)
    {
        var audiences = new List<AudienceModel>();
        var blocks = new List<AvailabilityBlockModel>();
        var gates = new List<ScopeGateModel>();
        var provided = new List<ContextValueModel>();

        for (var index = 0; index < compilations.Count; index++)
        {
            foreach (var type in catalogs[index].Types.Where(_ => _.TypeKind == TypeKind.Class && !_.IsAbstract))
            {
                var location = type.Locations.FirstOrDefault(_ => _.IsInSource)?.SourceTree?.FilePath ?? type.Name;
                if (Derives(type, WellKnownTypeNames.Audience))
                {
                    audiences.Add(new(type.Name, Documentation.SummaryOf(type)));
                }
                else if (Derives(type, WellKnownTypeNames.AvailabilityBlock))
                {
                    var (lines, reason) = AvailabilityTerms.LinesOf(type, compilations[index]);
                    if (lines is null)
                    {
                        diagnostics.Warning(
                            ScreenplayDiagnosticCodes.AvailabilityNotRepresentable,
                            $"The availability '{type.Name}' is left without rules: {reason}",
                            location);
                    }

                    blocks.Add(new(type.Name, Documentation.SummaryOf(type), lines ?? []));
                }
                else if (Derives(type, WellKnownTypeNames.AvailabilityScope))
                {
                    var scope = Scope(type);
                    gates.AddRange(AvailabilityReader.Gates(type).Select(_ => new ScopeGateModel(scope, _, location)));
                }

                provided.AddRange(ProvidedBy(type, location));
            }
        }

        return (
            [.. audiences.OrderBy(_ => _.Name, StringComparer.Ordinal)],
            [.. blocks.OrderBy(_ => _.Name, StringComparer.Ordinal)],
            gates,
            provided);
    }

    static List<string> Scope(INamedTypeSymbol type) =>
        type.ContainingNamespace.IsGlobalNamespace ? [] : [.. type.ContainingNamespace.ToDisplayString().Split('.')];

    static bool Derives(INamedTypeSymbol type, string fullMetadataName)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Is(fullMetadataName))
            {
                return true;
            }
        }

        return false;
    }

    static IEnumerable<ContextValueModel> ProvidedBy(INamedTypeSymbol type, string location)
    {
        foreach (var attribute in type.GetAttributes(WellKnownTypeNames.ProvideContextAttribute))
        {
            if (attribute.AttributeClass is not { TypeArguments.Length: 1 } attributeClass ||
                attribute.ConstructorArguments.Length < 2 ||
                attribute.ConstructorArguments[0].Value is not string query ||
                attribute.ConstructorArguments[1].Value is not string key)
            {
                continue;
            }

            var required = attribute.NamedArguments.FirstOrDefault(_ => _.Key == "Required").Value.Value is not false;
            var name = AvailabilityTerms.CamelCase(attributeClass.TypeArguments[0].Name);

            yield return new(name, query, key, Scope(type), required, location);
        }
    }
}
