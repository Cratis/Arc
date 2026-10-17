// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Represents the emission that remains once the protected reads Screenplay refuses were withheld.
/// </summary>
/// <param name="Emission">The emission to use.</param>
/// <param name="Diagnostics">The refusals and any defect found while admitting.</param>
internal sealed record ProtectedReadAdmitted(ScreenplayEmission Emission, IReadOnlyList<ScreenplayDiagnostic> Diagnostics);
