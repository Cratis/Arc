// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Represents everything generating a Screenplay document produced.
/// </summary>
/// <param name="Source">The <c>.play</c> text, empty when generation failed outright.</param>
/// <param name="Model">The model the document was generated from.</param>
/// <param name="Diagnostics">Everything that could not be expressed, reported rather than dropped.</param>
public record ScreenplayGenerationResult(
    string Source,
    ApplicationModel Model,
    IReadOnlyList<ScreenplayDiagnostic> Diagnostics)
{
    /// <summary>
    /// Gets a value indicating whether the document was generated without anything being reported as an error.
    /// </summary>
    public bool IsSuccess => !Diagnostics.Any(_ => _.Severity == ScreenplayDiagnosticSeverity.Error);
}
