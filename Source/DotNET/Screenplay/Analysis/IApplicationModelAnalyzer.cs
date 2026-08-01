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

    /// <summary>
    /// Analyzes the projects an application is written as and recovers the model they describe together.
    /// </summary>
    /// <param name="compilations">The compilations to analyze.</param>
    /// <param name="options">The options to analyze with, with every value already resolved.</param>
    /// <returns>The <see cref="ApplicationModelAnalysis"/>.</returns>
    /// <remarks>
    /// Each compilation contributes what its own assembly declares and the results are joined into one model: a
    /// namespace two projects declare into is one slice, a concept is declared once however many projects refer to
    /// it, and an event a sibling project declares is one the application has rather than one it imports. The order
    /// the list arrives in never reaches the model - the projects are put into assembly name order first.
    /// </remarks>
    ApplicationModelAnalysis Analyze(IReadOnlyList<Compilation> compilations, ScreenplayOptions options);
}
