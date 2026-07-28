// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Defines a system that generates a Screenplay document from the source of an application.
/// </summary>
/// <remarks>
/// The generator never loads an MSBuild workspace - the caller supplies the <see cref="Compilation"/>. That single
/// seam is what lets the same generator run from a command line tool, from an MSBuild task, from an analyzer and
/// from a specification built with <c>CSharpCompilation.Create</c>.
/// </remarks>
public interface IScreenplayGenerator
{
    /// <summary>
    /// Generates the Screenplay document describing an application.
    /// </summary>
    /// <param name="compilation">The compilation to generate from.</param>
    /// <param name="options">The options to generate with.</param>
    /// <returns>The <see cref="ScreenplayGenerationResult"/>.</returns>
    ScreenplayGenerationResult Generate(Compilation compilation, ScreenplayOptions options);
}
