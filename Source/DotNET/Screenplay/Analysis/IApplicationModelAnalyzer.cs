// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Defines a system that recovers the application model from the source of an application.
/// </summary>
/// <remarks>
/// This is the seam between the two halves of the generator. Everything on this side of it works with Roslyn
/// symbols and syntax; everything on the other side works only with the model, which is what makes the emitted
/// document reproducible and the emission half testable without a compiler.
/// </remarks>
public interface IApplicationModelAnalyzer
{
    /// <summary>
    /// Analyzes a compilation and recovers the model it describes.
    /// </summary>
    /// <param name="compilation">The compilation to analyze.</param>
    /// <param name="options">The options to analyze with, with every value already resolved.</param>
    /// <returns>The <see cref="ApplicationModelAnalysis"/>.</returns>
    ApplicationModelAnalysis Analyze(Compilation compilation, ScreenplayOptions options);
}
