// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Puts the compilations an application is analyzed from into the order everything else reads them in.
/// </summary>
/// <remarks>
/// An application written as several projects is handed over as a list, and nothing decides what order a host builds
/// that list in - a solution enumerates its projects in whatever order its file happens to name them, and a folder
/// scan in whatever order the file system answers. A document that reordered itself with the list would be no more
/// worth committing than one that reordered itself between builds, so the list is ordered here once and every union
/// downstream is a concatenation in this order.
/// <para>
/// Assemblies are named uniquely within an application, so the name is the whole of the key in practice. The
/// ordinally first source file breaks a tie anyway, because two compilations of one assembly - the same project built
/// for two target frameworks - is a list a host can hand over without meaning to.
/// </para>
/// </remarks>
public static class AnalyzedCompilations
{
    /// <summary>
    /// Orders the compilations an application is analyzed from.
    /// </summary>
    /// <param name="compilations">The compilations to order.</param>
    /// <returns>The compilations, ordered by the name of the assembly each one builds.</returns>
    public static IReadOnlyList<Compilation> Ordered(IEnumerable<Compilation> compilations) =>
    [
        .. compilations
            .OrderBy(_ => _.AssemblyName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(FirstFileOf, StringComparer.Ordinal)
    ];

    /// <summary>
    /// Gets the name the application falls back to when nothing configures one.
    /// </summary>
    /// <param name="compilations">The compilations being analyzed.</param>
    /// <returns>The name, or <see langword="null"/> when no single assembly names the application.</returns>
    /// <remarks>
    /// One project is the application, so the assembly it builds names it. Several projects are the application
    /// together and none of them names it - picking one would put the name of a layer where the name of the
    /// application belongs, and which layer got picked would be an accident of the alphabet. Nothing is offered
    /// instead, so a caller that says what the application is called is answered and one that does not gets the
    /// neutral default rather than a wrong name that looks deliberate.
    /// </remarks>
    public static string? NameOf(IReadOnlyList<Compilation> compilations) =>
        compilations.Count == 1 ? compilations[0].AssemblyName : null;

    /// <summary>
    /// Gets the ordinally first file a compilation was built from.
    /// </summary>
    /// <param name="compilation">The compilation to read.</param>
    /// <returns>The path, empty when it was built from none.</returns>
    static string FirstFileOf(Compilation compilation) =>
        compilation.SyntaxTrees.Select(_ => _.FilePath).Order(StringComparer.Ordinal).FirstOrDefault() ?? string.Empty;
}
