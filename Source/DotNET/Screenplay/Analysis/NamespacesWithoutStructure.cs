// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Reports a namespace that carries nothing to arrange the document by.
/// </summary>
/// <remarks>
/// Artifacts sitting in the root namespace leave the module, the feature and the slice with nothing to be named after
/// but the assembly, so all of them end up saying the same word. Naming them anything else would be fiction - the
/// source really does say nothing about where they belong - so this says what would fix it instead, which is either a
/// namespace per slice or a leading segment skipped.
/// </remarks>
public static class NamespacesWithoutStructure
{
    /// <summary>
    /// Reports every slice whose namespace carries no feature or slice to arrange by.
    /// </summary>
    /// <param name="slices">The slices to check.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <param name="segmentsToSkip">The number of leading namespace segments being skipped.</param>
    /// <remarks>
    /// The number of segments to skip is one number for the whole application rather than one per project. It says
    /// how the namespaces of the application are shaped, and the projects of one application share the root of their
    /// namespaces - a value per project would have to be keyed on assembly names the caller configuring it has no
    /// reason to know.
    /// </remarks>
    public static void Report(
        IEnumerable<SliceModel> slices,
        ScreenplayDiagnostics diagnostics,
        int segmentsToSkip)
    {
        foreach (var slice in slices.Where(_ => Segments(_.Namespace, segmentsToSkip) <= 1))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.NamespaceWithoutStructure,
                "The namespace carries no feature or slice to arrange by, so the module, the feature and the slice all take the same name - give the slice a namespace of its own, or skip a leading segment",
                slice.Namespace);
        }
    }

    /// <summary>
    /// Counts the namespace segments left to arrange a slice by.
    /// </summary>
    /// <param name="namespace">The namespace to count.</param>
    /// <param name="segmentsToSkip">The number of leading segments being skipped.</param>
    /// <returns>The number of segments.</returns>
    static int Segments(string @namespace, int segmentsToSkip) =>
        @namespace.Split('.', StringSplitOptions.RemoveEmptyEntries).Length - segmentsToSkip;
}
