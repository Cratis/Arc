// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Screens;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Slices;

/// <summary>
/// Turns everything declared within one namespace into a slice.
/// </summary>
/// <param name="readers">The <see cref="ArtifactReaders"/> reading each kind of artifact.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <param name="screens">The <see cref="ScreenReader"/> reading what the slice ends in.</param>
/// <param name="recovered">The <see cref="RecoveredArtifacts"/> holding what each declaration yielded.</param>
/// <remarks>
/// A namespace is the unit a slice is recovered from, because that is what the vertical slice convention makes it -
/// everything belonging to one behavior lives together, and nothing else does. The same convention puts the file
/// realizing a screen in that folder too, which is why screens are read here rather than discovered separately.
/// </remarks>
public class SliceReader(
    ArtifactReaders readers,
    ScreenplayDiagnostics diagnostics,
    ScreenReader screens,
    RecoveredArtifacts recovered)
{
    readonly SliceArtifactReader _artifacts = new(readers, diagnostics, recovered);

    /// <summary>
    /// Reads the slice a namespace declares.
    /// </summary>
    /// <param name="namespace">The namespace to read.</param>
    /// <param name="types">The types the namespace declares, in a deterministic order.</param>
    /// <returns>The <see cref="SliceModel"/>, or <see langword="null"/> when the namespace declares no artifact.</returns>
    public SliceModel? Read(string @namespace, IEnumerable<INamedTypeSymbol> types)
    {
        var content = new SliceContents();

        foreach (var type in types)
        {
            _artifacts.Read(type, @namespace, content);
        }

        if (content.IsEmpty)
        {
            return null;
        }

        var commands = content.Commands.OrderBy(_ => _.Name, StringComparer.Ordinal).ToList();
        var reactors = content.Reactors.OrderBy(_ => _.Name, StringComparer.Ordinal).ToList();
        var queries = QueryNames.Resolve(content.Queries, diagnostics, @namespace).OrderBy(_ => _.Name, StringComparer.Ordinal).ToList();

        return new(
            @namespace,
            NameOf(@namespace),
            SliceKindInference.Infer(commands, reactors, content.HasAggregateRoot),
            null,
            commands,
            [.. content.Events.OrderBy(_ => _.Name, StringComparer.Ordinal)],
            queries,
            content.Projection,
            reactors,
            [.. content.Constraints.OrderBy(_ => _.Name, StringComparer.Ordinal)])
        {
            Screens = [.. screens.Read(@namespace, types, queries)]
        };
    }

    /// <summary>
    /// Gets the name of a slice from the namespace it lives in.
    /// </summary>
    /// <param name="namespace">The namespace to name.</param>
    /// <returns>The name.</returns>
    static string NameOf(string @namespace)
    {
        var segments = @namespace.Split('.', StringSplitOptions.RemoveEmptyEntries);

        return segments.Length == 0 ? string.Empty : segments[^1];
    }
}
