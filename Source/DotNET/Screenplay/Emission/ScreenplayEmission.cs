// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission;

/// <summary>
/// Represents everything emitting a Screenplay document produced.
/// </summary>
/// <param name="Source">The printed <c>.play</c> text.</param>
/// <param name="Application">The document that was printed.</param>
/// <param name="Diagnostics">Everything that could not be expressed, reported rather than dropped.</param>
public record ScreenplayEmission(
    string Source,
    ApplicationSyntax Application,
    IReadOnlyList<ScreenplayDiagnostic> Diagnostics);
