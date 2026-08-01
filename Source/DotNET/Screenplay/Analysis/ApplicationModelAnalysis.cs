// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Represents the outcome of analyzing a compilation.
/// </summary>
/// <param name="Model">The model recovered from the source.</param>
/// <param name="Diagnostics">Everything the source declared that could not be recovered.</param>
public record ApplicationModelAnalysis(ApplicationModel Model, IReadOnlyList<ScreenplayDiagnostic> Diagnostics)
{
    /// <summary>
    /// Creates an analysis carrying a model and nothing to report.
    /// </summary>
    /// <param name="model">The model recovered from the source.</param>
    /// <returns>The <see cref="ApplicationModelAnalysis"/>.</returns>
    public static ApplicationModelAnalysis For(ApplicationModel model) => new(model, []);
}
