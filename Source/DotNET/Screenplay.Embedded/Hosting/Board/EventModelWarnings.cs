// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Collects what the board cannot hold, while the visitors that drop it are doing the dropping.
/// </summary>
/// <remarks>
/// The visitors pick the fields the board draws, so anything they do not read is dropped by default. Every
/// drop is recorded here and served beside the document, so a reader can see that the board shows less than
/// the source says instead of being told nothing. These are warnings, never errors - the source compiled.
/// </remarks>
public sealed class EventModelWarnings
{
    readonly List<Diagnostic> _warnings = [];

    /// <summary>
    /// Gets everything the board does not hold, in the order it was found.
    /// </summary>
    public IReadOnlyList<Diagnostic> All => _warnings;

    /// <summary>
    /// Reports that a number of declarations of one kind were not drawn.
    /// </summary>
    /// <param name="code">The code to report the warning under.</param>
    /// <param name="path">The path to where the declarations sit.</param>
    /// <param name="count">How many of them there were.</param>
    /// <param name="what">What was declared, in the singular.</param>
    /// <param name="because">Why the board cannot hold it.</param>
    /// <param name="location">Where in the source it sits.</param>
    public void Dropped(string code, string path, int count, string what, string because, SourceLocation location)
    {
        if (count <= 0)
        {
            return;
        }

        _warnings.Add(Diagnostic.Warning(
            code,
            $"{path}: {count} {what}{(count == 1 ? string.Empty : "s")} declared here {(count == 1 ? "was" : "were")} not added - {because}.",
            location));
    }

    /// <summary>
    /// Reports that something present in the source was not drawn.
    /// </summary>
    /// <param name="code">The code to report the warning under.</param>
    /// <param name="present">Whether the thing is present at all; nothing is reported when it is not.</param>
    /// <param name="path">The path to where it sits.</param>
    /// <param name="message">What was not drawn, and why.</param>
    /// <param name="location">Where in the source it sits.</param>
    public void Present(string code, bool present, string path, string message, SourceLocation location)
    {
        if (!present)
        {
            return;
        }

        _warnings.Add(Diagnostic.Warning(code, $"{path}: {message}.", location));
    }
}
