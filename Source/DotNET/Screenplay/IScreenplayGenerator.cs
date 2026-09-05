// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Defines a system that generates a Screenplay document from the source of an application.
/// </summary>
/// <remarks>
/// The generator never loads an MSBuild workspace - the caller supplies a <see cref="Compilation"/> or a
/// <see cref="DotNetProjectCompilation"/> carrying host-owned source context. That seam lets the same generator run
/// from a command line tool, from an MSBuild task, from an analyzer and from a specification built with
/// <c>CSharpCompilation.Create</c>.
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
    /// Generates the Screenplay document describing one project-aware application source set.
    /// </summary>
    /// <param name="project">The project compilation and its host-owned source context.</param>
    /// <param name="options">The options to generate with.</param>
    /// <returns>The <see cref="ScreenplayGenerationResult"/>.</returns>
    ScreenplayGenerationResult Generate(DotNetProjectCompilation project, ScreenplayOptions options);

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

    /// <summary>
    /// Generates the Screenplay document describing a project-aware application source set.
    /// </summary>
    /// <param name="projects">The project compilations and their host-owned source contexts.</param>
    /// <param name="options">The options to generate with.</param>
    /// <returns>The <see cref="ScreenplayGenerationResult"/>.</returns>
    /// <remarks>
    /// The compatibility document retains the established compilation-only behavior. Project roles and source
    /// contexts travel with this overload so hosts can use the neutral adapters without reconstructing them.
    /// </remarks>
    ScreenplayGenerationResult Generate(IReadOnlyList<DotNetProjectCompilation> projects, ScreenplayOptions options);
}
