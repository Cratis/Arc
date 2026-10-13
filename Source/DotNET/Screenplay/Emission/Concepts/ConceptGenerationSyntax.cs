// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Concepts;

/// <summary>
/// Admits concept-owned strategies at the executable version boundary.
/// </summary>
internal static class ConceptGenerationSyntax
{
    /// <summary>
    /// Builds a non-default generation line when its contract is executable.
    /// </summary>
    /// <param name="concept">The concept owning the strategy.</param>
    /// <param name="cap">The requested executable version cap.</param>
    /// <param name="diagnostics">Where withheld generation is reported.</param>
    /// <returns>The strategy syntax, or null when implicit or withheld.</returns>
    public static GenerationSyntax? Build(ConceptModel concept, SemanticVersion? cap, ScreenplayDiagnostics diagnostics)
    {
        if (concept.Generation is not { } generation || IsImplicit(concept, generation))
        {
            return null;
        }
        var reason = Failure(concept, cap);
        if (reason is not null)
        {
            diagnostics.Information(ScreenplayDiagnosticCodes.GenerationNotRecoverable, $"Concept '{concept.Name}': {reason}; its generate line remains in code", concept.GenerationLocation ?? concept.Name);
            return null;
        }

        return new(generation.Kind, SourceLocation.Start)
        {
            Prefix = generation.Prefix,
            Case = generation.Case,
            Size = generation.Size,
            Alphabet = generation.Alphabet,
            Count = generation.Count,
            Encoding = generation.Encoding
        };
    }

    static bool IsImplicit(ConceptModel concept, GenerationModel generation) => concept.Primitive == ScreenplayPrimitive.Uuid && generation.Kind == "uuid";

    static string? Failure(ConceptModel concept, SemanticVersion? cap)
    {
        if (cap is { } version && !version.IsAtLeast(SemanticVersion.V15))
        {
            return $"generation requires ESM v15, but ScreenplayOptions.MaximumExecutableModelVersion is capped at ESM v{cap}";
        }

        return concept.Validations.Any() || concept.IsPii || concept.IsSensitive
            ? "Screenplay cannot execute generation with concept validation rules or compliance markers yet (Cratis/Screenplay#616)"
            : null;
    }
}
