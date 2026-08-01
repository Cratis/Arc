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

    /// <summary>
    /// Generates the Screenplay document describing an application written as several projects.
    /// </summary>
    /// <param name="compilations">The compilations to generate from.</param>
    /// <param name="options">The options to generate with.</param>
    /// <returns>The <see cref="ScreenplayGenerationResult"/>.</returns>
    /// <remarks>
    /// A layered application - domain, application and api, or a host beside the bounded contexts it serves - is not
    /// described by any one of its projects. Each compilation contributes what its own assembly declares and the
    /// document holds all of it: a namespace two projects declare into is one slice, a concept referred to from three
    /// of them is declared once, and an event a sibling project declares is one the application has rather than one
    /// it imports.
    /// <para>
    /// The order the list arrives in does not reach the document. Nothing decides what order a host enumerates the
    /// projects of a solution in, so they are put into assembly name order before anything is read and the same
    /// projects always print the same bytes.
    /// </para>
    /// </remarks>
    ScreenplayGenerationResult Generate(IReadOnlyList<Compilation> compilations, ScreenplayOptions options);
}
