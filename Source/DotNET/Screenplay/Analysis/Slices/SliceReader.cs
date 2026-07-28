// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Slices;

/// <summary>
/// Turns everything declared within one namespace into a slice.
/// </summary>
/// <param name="readers">The <see cref="ArtifactReaders"/> reading each kind of artifact.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// A namespace is the unit a slice is recovered from, because that is what the vertical slice convention makes it -
/// everything belonging to one behavior lives together, and nothing else does.
/// </remarks>
public class SliceReader(ArtifactReaders readers, ScreenplayDiagnostics diagnostics)
{
    readonly SliceArtifactReader _artifacts = new(readers, diagnostics);

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

        return new(
            @namespace,
            NameOf(@namespace),
            SliceKindInference.Infer(commands, reactors),
            null,
            commands,
            [.. content.Events.OrderBy(_ => _.Name, StringComparer.Ordinal)],
            [.. QueryNames.Resolve(content.Queries, diagnostics, @namespace).OrderBy(_ => _.Name, StringComparer.Ordinal)],
            content.Projection,
            reactors,
            [.. content.Constraints.OrderBy(_ => _.Name, StringComparer.Ordinal)]);
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
