// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Collects the diagnostics produced while a document is generated.
/// </summary>
/// <remarks>
/// Silent loss is what makes generated output impossible to trust, so every construct that cannot be expressed is
/// recorded here rather than quietly dropped. One collector is used per generation, which keeps generation
/// reentrant.
/// </remarks>
public class ScreenplayDiagnostics
{
    readonly List<ScreenplayDiagnostic> _diagnostics = [];

    /// <summary>
    /// Gets everything collected so far, in the order it was reported.
    /// </summary>
    public IReadOnlyList<ScreenplayDiagnostic> All => _diagnostics;

    /// <summary>
    /// Reports something worth knowing that does not affect the document.
    /// </summary>
    /// <param name="code">The code identifying the kind of diagnostic.</param>
    /// <param name="message">What happened.</param>
    /// <param name="location">Where it happened.</param>
    public void Information(string code, string message, string? location = null) =>
        _diagnostics.Add(new(ScreenplayDiagnosticSeverity.Information, code, message, location));

    /// <summary>
    /// Reports something that could not be expressed and was left out of the document.
    /// </summary>
    /// <param name="code">The code identifying the kind of diagnostic.</param>
    /// <param name="message">What happened.</param>
    /// <param name="location">Where it happened.</param>
    public void Warning(string code, string message, string? location = null) =>
        _diagnostics.Add(new(ScreenplayDiagnosticSeverity.Warning, code, message, location));

    /// <summary>
    /// Reports something that stopped the document from being generated.
    /// </summary>
    /// <param name="code">The code identifying the kind of diagnostic.</param>
    /// <param name="message">What happened.</param>
    /// <param name="location">Where it happened.</param>
    public void Error(string code, string message, string? location = null) =>
        _diagnostics.Add(new(ScreenplayDiagnosticSeverity.Error, code, message, location));

    /// <summary>
    /// Adds everything from another set of diagnostics.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to add.</param>
    public void AddRange(IEnumerable<ScreenplayDiagnostic> diagnostics) => _diagnostics.AddRange(diagnostics);
}
