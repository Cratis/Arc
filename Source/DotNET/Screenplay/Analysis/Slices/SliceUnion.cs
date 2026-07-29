// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Slices;

/// <summary>
/// Joins what the projects of an application declare into one slice per namespace.
/// </summary>
/// <remarks>
/// A slice is recovered from a namespace, and nothing says a namespace belongs to one project. A bounded context that
/// publishes its events from a contracts project and handles its commands from the project beside it declares one
/// slice from two compilations, and a document holding both of them separately would say the slice twice and describe
/// half of it in each. So the parts are joined: the events of one and the commands of the other end up in the slice
/// they were always written for.
/// <para>
/// Everything within a slice is named once, though, so what two projects declare under one name cannot both be
/// described. The parts arrive in the order the projects are read, which is their assembly name order and the only
/// order there is to prefer by, and the first of a name is kept.
/// </para>
/// </remarks>
public static class SliceUnion
{
    /// <summary>
    /// Joins the slices of every project into the slices of the application.
    /// </summary>
    /// <param name="slices">The slices each project declared, in the order the projects were read.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>The slices, one per namespace, ordered by namespace.</returns>
    public static IReadOnlyList<SliceModel> Of(IEnumerable<SliceModel> slices, ScreenplayDiagnostics diagnostics) =>
    [
        .. slices
            .GroupBy(_ => _.Namespace, StringComparer.Ordinal)
            .OrderBy(_ => _.Key, StringComparer.Ordinal)
            .Select(group => Join([.. group], diagnostics))
    ];

    /// <summary>
    /// Joins the scenarios every project specifies one slice by.
    /// </summary>
    /// <param name="specifications">The scenarios each project placed under the slice, in the order they were read.</param>
    /// <param name="namespace">The namespace of the slice.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>The scenarios kept, ordered by name.</returns>
    /// <remarks>
    /// Scenarios arrive apart from the rest of a slice, because which slice one belongs to is only known once every
    /// project has been read, so they are joined here rather than in <see cref="Join"/>. What holds for every other
    /// part of a slice holds for them: a document saying <c>specification and_the_name_is_taken</c> twice in one slice
    /// says the same word twice and means it differently, so the first is kept.
    /// </remarks>
    public static IReadOnlyList<SpecificationModel> Specifications(
        IEnumerable<SpecificationModel> specifications,
        string @namespace,
        ScreenplayDiagnostics diagnostics) =>
        Once(specifications, _ => _.Name, "specification", @namespace, diagnostics);

    /// <summary>
    /// Joins the parts of one slice.
    /// </summary>
    /// <param name="parts">The parts, in the order the projects declaring them were read.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>The slice.</returns>
    /// <remarks>
    /// A namespace declared by a single project is that project's slice exactly as it read it, which is what an
    /// application of one project has always produced.
    /// </remarks>
    static SliceModel Join(IReadOnlyList<SliceModel> parts, ScreenplayDiagnostics diagnostics)
    {
        if (parts.Count == 1)
        {
            return parts[0];
        }

        var @namespace = parts[0].Namespace;

        return new(
            @namespace,
            parts[0].Name,
            SliceKindInference.Combine(parts.Select(_ => _.Kind)),
            parts.Select(_ => _.Description).FirstOrDefault(_ => _ is not null),
            Once(parts.SelectMany(_ => _.Commands), _ => _.Name, "command", @namespace, diagnostics),
            Once(parts.SelectMany(_ => _.Events), _ => _.Name, "event", @namespace, diagnostics),
            Once(parts.SelectMany(_ => _.Queries), _ => _.Name, "query", @namespace, diagnostics),
            OneProjection(parts, @namespace, diagnostics),
            Once(parts.SelectMany(_ => _.Reactors), _ => _.Name, "reactor", @namespace, diagnostics),
            Once(parts.SelectMany(_ => _.Constraints), _ => _.Name, "constraint", @namespace, diagnostics))
        {
            Screens = Once(parts.SelectMany(_ => _.Screens), _ => _.Name, "screen", @namespace, diagnostics)
        };
    }

    /// <summary>
    /// Keeps the first artifact declared under each name, reporting every one that repeats a name.
    /// </summary>
    /// <typeparam name="T">The kind of artifact.</typeparam>
    /// <param name="declared">Everything the projects declared, in the order they were read.</param>
    /// <param name="name">How an artifact is named.</param>
    /// <param name="kind">What an artifact of this kind is called, for use in the report.</param>
    /// <param name="namespace">The namespace of the slice.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>The artifacts kept, ordered by name.</returns>
    static IReadOnlyList<T> Once<T>(
        IEnumerable<T> declared,
        Func<T, string> name,
        string kind,
        string @namespace,
        ScreenplayDiagnostics diagnostics)
    {
        var taken = new HashSet<string>(StringComparer.Ordinal);
        var kept = new List<T>();

        foreach (var artifact in declared)
        {
            if (taken.Add(name(artifact)))
            {
                kept.Add(artifact);

                continue;
            }

            diagnostics.Warning(
                ScreenplayDiagnosticCodes.RepeatedDeclarationAcrossProjects,
                $"'{name(artifact)}' is a {kind} a second project of the application declares in this slice, and a slice describes one of a name, so it was left out",
                @namespace);
        }

        return [.. kept.OrderBy(name, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Keeps the single projection a slice may declare, reporting any beyond the first.
    /// </summary>
    /// <param name="parts">The parts, in the order the projects declaring them were read.</param>
    /// <param name="namespace">The namespace of the slice.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>The projection, or <see langword="null"/> when no part declares one.</returns>
    static ProjectionModel? OneProjection(
        IReadOnlyList<SliceModel> parts,
        string @namespace,
        ScreenplayDiagnostics diagnostics)
    {
        ProjectionModel? kept = null;

        foreach (var projection in parts.Select(_ => _.Projection).OfType<ProjectionModel>())
        {
            if (kept is null)
            {
                kept = projection;

                continue;
            }

            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableProjectionConstruct,
                $"'{projection.Identifier}' is a second projection in one slice, and a slice may declare at most one, so it was left out",
                @namespace);
        }

        return kept;
    }
}
