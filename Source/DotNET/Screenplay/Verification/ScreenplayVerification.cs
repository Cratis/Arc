// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Verification;

/// <summary>
/// Represents everything reading a printed Screenplay document back produced.
/// </summary>
/// <param name="Source">The printed <c>.play</c> text that was compiled.</param>
/// <param name="Application">The document the text compiled to, null when it did not compile at all.</param>
/// <param name="Diagnostics">Everything the Screenplay compiler reported about the text.</param>
public record ScreenplayVerification(
    string Source,
    ApplicationSyntax? Application,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>
    /// Gets the diagnostics from compiling and binding the generated document to an executable semantic model.
    /// </summary>
    public IReadOnlyList<Diagnostic> BindingDiagnostics { get; init; } = [];

    /// <summary>
    /// Gets everything the Screenplay compiler reported as an error.
    /// </summary>
    public IReadOnlyList<Diagnostic> Errors => [.. Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error)];

    /// <summary>
    /// Gets a value indicating whether the printed text compiles.
    /// </summary>
    public bool Compiles => Errors.Count == 0;

    /// <summary>
    /// Gets unexpected semantic binding errors in either generation mode.
    /// </summary>
    /// <returns>Every binding error except expected admission and legacy read-consistency diagnostics.</returns>
    public IReadOnlyList<Diagnostic> UnexpectedBindingErrors() =>
        [.. BindingDiagnostics.Where(diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error &&
            diagnostic.Code != "PLAY0271" &&
            diagnostic.Code != "PLAY0268")];
}
