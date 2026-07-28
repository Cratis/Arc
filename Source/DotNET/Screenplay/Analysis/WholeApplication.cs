// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Cratis.Arc.Screenplay.Analysis.Screens;
using Cratis.Arc.Screenplay.Analysis.Types;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Holds everything an analysis keeps for the whole application rather than for one of its projects.
/// </summary>
/// <param name="Compilations">The compilations the application is analyzed from, ordered.</param>
/// <param name="Diagnostics">The <see cref="ScreenplayDiagnostics"/> everything is reported to.</param>
/// <remarks>
/// An application written as one project made this distinction invisible: everything a compilation was read into was
/// the application, because the compilation was. With several, each of these has to be one thing across all of them
/// or the document says something twice or misses it entirely. A concept is declared once and referred to by name
/// from there on; an aggregate root one project declares is handed its work by a command another project holds; a
/// screen imports the query of a slice that may live in a different project; a body reached from a handler may be
/// written in the project below it; and diagnostics are one sequence in one order.
/// <para>
/// Everything not here is per project, because it reads a declaration the project itself catalogued and a symbol
/// belongs to the compilation it came from.
/// </para>
/// </remarks>
public record WholeApplication(IReadOnlyList<Compilation> Compilations, ScreenplayDiagnostics Diagnostics)
{
    /// <summary>
    /// Gets the <see cref="IUserInterfaceFiles"/> the screens of a slice are found through.
    /// </summary>
    public IUserInterfaceFiles Files { get; init; } = new UserInterfaceFiles();

    /// <summary>
    /// Gets the models every syntax tree of the application is read through.
    /// </summary>
    public SemanticModels Models { get; } = new(Compilations);

    /// <summary>
    /// Gets the registry collecting every concept the application refers to.
    /// </summary>
    public TypeRegistry Types { get; } = new();

    /// <summary>
    /// Gets the aggregate roots the application declares, and which of them a command reaches.
    /// </summary>
    public AggregateRootCatalog AggregateRoots { get; } = new();

    /// <summary>
    /// Gets the queries screens read through that a slice other than their own declares.
    /// </summary>
    public CrossSliceQueries Elsewhere { get; } = new();

    /// <summary>
    /// Creates what an application of a single project holds.
    /// </summary>
    /// <param name="compilation">The compilation being analyzed.</param>
    /// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> everything is reported to.</param>
    /// <returns>The <see cref="WholeApplication"/>.</returns>
    public static WholeApplication Of(Compilation compilation, ScreenplayDiagnostics diagnostics) =>
        new([compilation], diagnostics);
}
