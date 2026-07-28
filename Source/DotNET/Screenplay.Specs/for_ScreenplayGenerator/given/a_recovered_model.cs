// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Stands in for the analysis half, returning a model that was prepared rather than recovered.
/// </summary>
/// <param name="model">The model to return.</param>
/// <param name="diagnostics">What analysis reports alongside the model.</param>
public class a_recovered_model(ApplicationModel model, params ScreenplayDiagnostic[] diagnostics) : IApplicationModelAnalyzer
{
    /// <summary>
    /// Gets the options the generator handed to analysis.
    /// </summary>
    public ScreenplayOptions? Options { get; private set; }

    /// <inheritdoc/>
    public ApplicationModelAnalysis Analyze(Compilation compilation, ScreenplayOptions options)
    {
        Options = options;

        return new(model, diagnostics);
    }
}
