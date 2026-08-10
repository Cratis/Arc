// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Holds every scenario the application specifies its slices by, arranged under the slice each belongs to.
/// </summary>
/// <remarks>
/// Specifications are read once the slices are in, because which slice a scenario belongs to is answered by which
/// namespaces turned out to declare one. Reading them alongside would mean deciding that against namespaces still
/// being read, and a scenario would land under a different slice depending on the order the compilation was walked.
/// </remarks>
public class SpecificationCatalog
{
    readonly Dictionary<string, List<SpecificationModel>> _bySlice;

    SpecificationCatalog(Dictionary<string, List<SpecificationModel>> bySlice) => _bySlice = bySlice;

    /// <summary>
    /// Reads every specification the compilation declares.
    /// </summary>
    /// <param name="compilation">The compilation being analyzed.</param>
    /// <param name="catalog">The catalogue of everything the compilation declares.</param>
    /// <param name="slices">The namespaces a slice was recovered from.</param>
    /// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unreadable is reported to.</param>
    /// <returns>The <see cref="SpecificationCatalog"/>.</returns>
    public static SpecificationCatalog Read(
        Compilation compilation,
        ArtifactCatalog catalog,
        IEnumerable<string> slices,
        ScreenplayDiagnostics diagnostics)
    {
        var reader = new SpecificationReader(compilation, diagnostics);
        var placement = new SpecificationPlacement(slices);
        var bySlice = new Dictionary<string, List<SpecificationModel>>(StringComparer.Ordinal);

        foreach (var type in catalog.Types)
        {
            if (!reader.IsSpecification(type))
            {
                Report(reader.ScenarioWithoutCounterpart(type), type, diagnostics);
                continue;
            }

            if (placement.SliceOf(type) is not { } slice)
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.UnreadableSpecification,
                    $"The scenario '{type.Name}' was left out because no namespace above it declares a slice for it to specify",
                    type.ToDisplayString());

                continue;
            }

            if (reader.Read(type, SpecificationPlacement.NameOf(type, slice)) is { } specification)
            {
                Add(bySlice, slice, specification);
            }
        }

        return new(bySlice);
    }

    /// <summary>
    /// Gets the specifications belonging to a slice.
    /// </summary>
    /// <param name="namespace">The namespace of the slice.</param>
    /// <returns>The specifications, ordered by name.</returns>
    public IEnumerable<SpecificationModel> For(string @namespace) =>
        _bySlice.TryGetValue(@namespace, out var specifications)
            ? specifications.OrderBy(_ => _.Name, StringComparer.Ordinal)
            : [];

    /// <summary>
    /// Reports a scenario a slice is specified by that the language has nowhere to put.
    /// </summary>
    /// <param name="scenario">The name of the scenario, or <see langword="null"/> when there is nothing to report.</param>
    /// <param name="type">The type declaring the specification.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <remarks>
    /// This is said before the specification is placed under a slice rather than after, because where it belongs is
    /// beside the point: nothing of it is going into the document either way, and a scenario nobody hears about is
    /// exactly as lost whichever namespace it sits in.
    /// </remarks>
    static void Report(string? scenario, INamedTypeSymbol type, ScreenplayDiagnostics diagnostics)
    {
        if (scenario is null)
        {
            return;
        }

        diagnostics.Warning(
            ScreenplayDiagnosticCodes.ScenarioWithoutCounterpart,
            $"The scenario '{type.Name}' is written as a {scenario}, which specifies the slice through something the language has nowhere to hold, so the whole of it was left out",
            type.ToDisplayString());
    }

    /// <summary>
    /// Adds a specification under the slice it belongs to.
    /// </summary>
    /// <param name="bySlice">The specifications collected so far.</param>
    /// <param name="slice">The namespace of the slice.</param>
    /// <param name="specification">The specification to add.</param>
    static void Add(Dictionary<string, List<SpecificationModel>> bySlice, string slice, SpecificationModel specification)
    {
        if (!bySlice.TryGetValue(slice, out var specifications))
        {
            specifications = [];
            bySlice[slice] = specifications;
        }

        specifications.Add(specification);
    }
}
