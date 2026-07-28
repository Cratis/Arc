// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Represents something the generator could not express, reported instead of being silently dropped.
/// </summary>
/// <param name="Severity">How serious the diagnostic is.</param>
/// <param name="Code">The stable code identifying the kind of diagnostic, for example <c>SP0001</c>.</param>
/// <param name="Message">What happened, in terms the reader can act on.</param>
/// <param name="Location">Where it happened - a namespace, a declaration name or a file path.</param>
public record ScreenplayDiagnostic(
    ScreenplayDiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Location)
{
    /// <inheritdoc/>
    public override string ToString() =>
        Location is null ? $"{Severity} {Code}: {Message}" : $"{Severity} {Code}: {Message} ({Location})";
}
