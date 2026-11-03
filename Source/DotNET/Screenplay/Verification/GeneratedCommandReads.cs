// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.Verification;

/// <summary>Preserves the generator's advisory-read decisions, including explicit version-cap demotions.</summary>
/// <param name="model">The model after emission decisions.</param>
/// <param name="options">The resolved emission options.</param>
/// <param name="naming">The emitter's name conversion.</param>
internal sealed class GeneratedCommandReads(ApplicationModel model, ScreenplayOptions options, IScreenplayNaming naming)
{
    /// <summary>Verifies printed source while retaining its analysis-derived read intent.</summary>
    /// <param name="verifier">The language verifier.</param>
    /// <param name="emission">The emission and its read context.</param>
    /// <returns>The verification with emission context.</returns>
    internal static ScreenplayVerification Verify(IScreenplayVerifier verifier, ScreenplayEmission emission) =>
        verifier.Verify(emission.Source) with { CommandReads = emission.CommandReads };

    /// <summary>Checks a diagnostic against its owning command's analyzed advisory aliases.</summary>
    /// <param name="verification">The parsed printed document.</param>
    /// <param name="diagnostic">The binding diagnostic.</param>
    /// <param name="operand">The condition operand, or null for a mapping refusal.</param>
    /// <returns>True only when the owning command intentionally emits the relevant advisory read.</returns>
    internal bool IsAdvisory(ScreenplayVerification verification, Diagnostic diagnostic, string? operand)
    {
        if (verification.Application is not { CommandReadMode: null } application ||
            EmittedCommandLocation.Find(application, diagnostic.Location.Line, model, options) is not { } owner)
        {
            return false;
        }

        var aliases = owner.Command.Authoring?.Reads.Where(read => !read.Protected)
            .Select(read => read.NamedByView ? naming.ToDeclarationName(read.Name) : naming.ToPropertyName(read.Alias))
            .Where(alias => owner.Printed.Reads?.Any(read => (read.Alias ?? read.ReadModel) == alias) == true).ToArray() ?? [];
        return aliases.Length > 0 && (operand is null || aliases.Contains(operand.Split('.')[0], StringComparer.Ordinal));
    }
}
