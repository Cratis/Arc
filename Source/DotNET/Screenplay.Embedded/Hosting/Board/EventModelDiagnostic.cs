// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents something the compiler, or the mapping onto the board, has to say about a document.
/// </summary>
/// <param name="Code">The code the message is reported under.</param>
/// <param name="Message">What it has to say.</param>
/// <param name="Line">The line in the source it is about.</param>
/// <param name="Column">The column in the source it is about.</param>
/// <param name="Path">The path to the source it is about, when the compiler knows one.</param>
public record EventModelDiagnostic(string Code, string Message, int Line, int Column, string? Path)
{
    /// <summary>
    /// Gets the diagnostic for what the compiler reported.
    /// </summary>
    /// <param name="diagnostic">The diagnostic the compiler reported.</param>
    /// <returns>The resulting <see cref="EventModelDiagnostic"/>.</returns>
    public static EventModelDiagnostic From(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        return new(
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.Location.Line,
            diagnostic.Location.Column,
            diagnostic.Location.Path);
    }
}
