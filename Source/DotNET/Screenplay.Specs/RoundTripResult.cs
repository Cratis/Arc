// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Represents the outcome of printing, compiling and reprinting a document.
/// </summary>
/// <param name="Printed">The printed source.</param>
/// <param name="Reprinted">The source printed from the recompiled document.</param>
/// <param name="Diagnostics">Everything the compiler reported.</param>
public record RoundTripResult(string Printed, string Reprinted, IEnumerable<Diagnostic> Diagnostics)
{
    /// <summary>
    /// Gets everything the compiler reported as an error.
    /// </summary>
    public IEnumerable<Diagnostic> Errors => Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Gets a value indicating whether the printed source compiles.
    /// </summary>
    public bool Compiles => !Errors.Any();

    /// <summary>
    /// Gets a value indicating whether the second print is identical to the first.
    /// </summary>
    public bool IsStable => string.Equals(Printed, Reprinted, StringComparison.Ordinal);

    /// <summary>
    /// Gets a description of everything the compiler reported, for use in assertion output.
    /// </summary>
    public string Report => string.Join('\n', Diagnostics.Select(_ => $"{_.Severity} {_.Location.Line}:{_.Location.Column} {_.Message}"));
}
