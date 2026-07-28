// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Reports the user interface files whose relationship to a slice is not certain.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> to report to.</param>
/// <remarks>
/// A screen is recovered from where a file sits rather than from anything the source states, so the answer is only
/// as good as the folder structure. Where the convention holds - one folder, one slice - it is exact. Where it does
/// not, saying so is the whole difference between a document a reader can act on and one they have to double check.
/// One instance is used per generation, because the folder a slice claims is only shared once another slice claims
/// it too.
/// </remarks>
public class AmbiguousScreens(ScreenplayDiagnostics diagnostics)
{
    readonly Dictionary<string, string> _claimed = new(StringComparer.Ordinal);

    /// <summary>
    /// Reports the directories a slice takes its screens from, when more than one slice or more than one folder is
    /// involved.
    /// </summary>
    /// <param name="namespace">The namespace of the slice.</param>
    /// <param name="directories">The directories the source of the slice lives in.</param>
    public void ReportDirectories(string @namespace, IReadOnlyList<string> directories)
    {
        if (directories.Count > 1)
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.AmbiguousScreenFile,
                $"The source of the slice is spread over {directories.Count} folders ({string.Join(", ", directories)}), so every user interface file in any of them is taken to be a screen of it",
                @namespace);
        }

        foreach (var directory in directories)
        {
            ReportShared(@namespace, directory);
        }
    }

    /// <summary>
    /// Reports a file left out because another file already named the screen.
    /// </summary>
    /// <param name="namespace">The namespace of the slice.</param>
    /// <param name="path">The path of the file left out.</param>
    /// <param name="kept">The path of the file that named the screen first.</param>
    /// <param name="name">The name both files give the screen.</param>
    public void ReportRepeatedName(string @namespace, string path, string kept, string name) =>
        diagnostics.Warning(
            ScreenplayDiagnosticCodes.AmbiguousScreenFile,
            $"'{path}' names the screen '{name}', which '{kept}' already names, so it was left out",
            @namespace);

    /// <summary>
    /// Reports a directory a second slice takes its screens from.
    /// </summary>
    /// <param name="namespace">The namespace of the slice.</param>
    /// <param name="directory">The directory to claim.</param>
    void ReportShared(string @namespace, string directory)
    {
        if (!_claimed.TryGetValue(directory, out var owner))
        {
            _claimed[directory] = @namespace;
            return;
        }

        diagnostics.Information(
            ScreenplayDiagnosticCodes.AmbiguousScreenFile,
            $"'{directory}' holds the source of '{owner}' as well, so every user interface file in it is taken to be a screen of both",
            @namespace);
    }
}
