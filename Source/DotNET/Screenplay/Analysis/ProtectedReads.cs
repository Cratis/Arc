// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Decides which command reads a document states as protected, from what only the source declares.
/// </summary>
/// <remarks>
/// Whether a read is protected is Arc's decision (the effective read protection, <c>[Unprotected]</c>, the key and its
/// absence guard) and is made by the reader. This keeps protected reads out of a document capped below ESM v25 and
/// withholds the advisory reads of a document that states the <c>command reads protected</c> preamble, where every
/// <c>reads</c> claims protection. Whether Screenplay admits a read is asked of the compiler by
/// <see cref="ProtectedReadAdmission"/>.
/// </remarks>
internal static class ProtectedReads
{
    /// <summary>
    /// Determines whether any command keeps a protected read.
    /// </summary>
    /// <param name="model">The application.</param>
    /// <returns>True when the document states the preamble.</returns>
    internal static bool Declares(ApplicationModel model) => model.Slices.SelectMany(slice => slice.Commands).Any(command => command.Authoring?.Reads.Any(read => read.Protected) == true);

    /// <summary>
    /// Keeps protected reads out of a document capped below the version that admits them.
    /// </summary>
    /// <param name="model">The application.</param>
    /// <param name="cap">The executable version cap.</param>
    /// <param name="diagnostics">The diagnostic collector.</param>
    /// <returns>The application with its reads advisory when the cap is below ESM v25.</returns>
    internal static ApplicationModel Cap(ApplicationModel model, SemanticVersion? cap, ScreenplayDiagnostics diagnostics)
    {
        if (cap is not { } version || version.IsAtLeast(SemanticVersion.V25) || !Declares(model))
        {
            return model;
        }

        return model with
        {
            Slices = model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(command =>
                {
                    if (command.Authoring?.Reads.Any(read => read.Protected) != true)
                    {
                        return command;
                    }

                    diagnostics.Warning(
                        ScreenplayDiagnosticCodes.UnprotectableCommandRead,
                        $"Command '{command.Name}' has protected reads, which require ESM v25, but ScreenplayOptions.MaximumExecutableModelVersion is capped at ESM v{version}; they are not stated as protected reads",
                        $"{slice.Namespace}.{command.Name}");

                    return command with { Authoring = command.Authoring with { Reads = command.Authoring.Reads.Select(read => read with { Protected = false }).ToList() } };
                }).ToList()
            }).ToList()
        };
    }

    /// <summary>
    /// Withholds the advisory reads of a document that states the protected preamble.
    /// </summary>
    /// <param name="model">The application.</param>
    /// <param name="diagnostics">The diagnostic collector.</param>
    /// <returns>The application without reads that cannot claim protection.</returns>
    internal static ApplicationModel WithoutAdvisory(ApplicationModel model, ScreenplayDiagnostics diagnostics) => !Declares(model)
        ? model
        : model with
        {
            Slices = model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(command => AuthoringDeclarations.WithoutReads(
                    command,
                    model,
                    command.Authoring?.Reads.Where(read => !read.Protected).Select(read => read.Alias).ToHashSet(StringComparer.Ordinal) ?? [],
                    diagnostics,
                    $"{slice.Namespace}.{command.Name}",
                    $"Command '{command.Name}': the document states protected command reads, where every read claims protection, so its advisory reads, requirements and mappings were left in code")).ToList()
            }).ToList()
        };
}
