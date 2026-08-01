// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.Verification;

/// <summary>
/// Represents the outcome of printing, compiling and reprinting a document.
/// </summary>
/// <param name="Verification">What reading the printed text back produced.</param>
/// <param name="Reprinted">The text printed from the recompiled document.</param>
public record RoundTripResult(ScreenplayVerification Verification, string Reprinted)
{
    /// <summary>
    /// Gets the printed text.
    /// </summary>
    public string Printed => Verification.Source;

    /// <summary>
    /// Gets everything the compiler reported.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics => Verification.Diagnostics;

    /// <summary>
    /// Gets everything the compiler reported as an error.
    /// </summary>
    public IReadOnlyList<Diagnostic> Errors => Verification.Errors;

    /// <summary>
    /// Gets a value indicating whether the printed text compiles.
    /// </summary>
    public bool Compiles => Verification.Compiles;

    /// <summary>
    /// Gets a value indicating whether the second print is identical to the first.
    /// </summary>
    public bool IsStable => string.Equals(Printed, Reprinted, StringComparison.Ordinal);

    /// <summary>
    /// Gets a description of everything the compiler reported, for use in assertion output.
    /// </summary>
    public string Report => string.Join('\n', Diagnostics.Select(_ => $"{_.Severity} {_.Location.Line}:{_.Location.Column} {_.Message}"));
}
