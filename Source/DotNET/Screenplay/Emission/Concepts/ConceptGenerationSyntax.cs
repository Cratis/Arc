// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Concepts;

/// <summary>
/// Admits concept-owned strategies at the executable version boundary.
/// </summary>
internal static class ConceptGenerationSyntax
{
    /// <summary>
    /// Builds a non-default generation line when its complete contract is executable.
    /// </summary>
    /// <param name="concept">The concept owning the strategy.</param>
    /// <param name="cap">The requested executable version cap.</param>
    /// <param name="diagnostics">Where withheld generation is reported.</param>
    /// <returns>The strategy syntax, or null when implicit or withheld.</returns>
    public static GenerationSyntax? Build(ConceptModel concept, SemanticVersion? cap, ScreenplayDiagnostics diagnostics)
    {
        if (concept.Generation is not { } generation)
        {
            return null;
        }
        var reason = GenerationAdmission.Failure(concept, cap);
        if (reason is not null)
        {
            diagnostics.Information(ScreenplayDiagnosticCodes.GenerationNotRecoverable, $"Concept '{concept.Name}': {reason}; its generation contract remains in code", concept.GenerationLocation ?? concept.Name);
            return null;
        }

        return GenerationAdmission.IsImplicit(concept) ? null : GenerationAdmission.Syntax(generation);
    }
}
