// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Screens;
using Cratis.Arc.Screenplay.Analysis.Slices;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Reads one project of an application into the slices it declares.
/// </summary>
/// <remarks>
/// Everything within this reads symbols and syntax of a single compilation, which is what makes it the unit an
/// application of several projects is read in. What comes out is joined to what the other projects yielded, and the
/// two questions that can only be answered once every project has been read - what a validator declares about a
/// concept another project holds, and how much the errors of this project left behind - are asked of this afterwards
/// rather than kept inside it.
/// </remarks>
public class CompilationAnalysis
{
    readonly ArtifactCatalog _catalog;
    readonly ArtifactReaders _readers;
    readonly RecoveredArtifacts _recovered;

    CompilationAnalysis(
        Compilation compilation,
        ArtifactCatalog catalog,
        ArtifactReaders readers,
        RecoveredArtifacts recovered,
        IReadOnlyList<SliceModel> slices)
    {
        Compilation = compilation;
        Slices = slices;
        _catalog = catalog;
        _readers = readers;
        _recovered = recovered;
    }

    /// <summary>
    /// Gets the compilation that was read.
    /// </summary>
    public Compilation Compilation { get; }

    /// <summary>
    /// Gets the slices the compilation declares, ordered by namespace.
    /// </summary>
    public IReadOnlyList<SliceModel> Slices { get; }

    /// <summary>
    /// Reads a compilation into the slices it declares.
    /// </summary>
    /// <param name="compilation">The compilation to read.</param>
    /// <param name="catalog">The catalogue of everything it declares.</param>
    /// <param name="paths">The <see cref="SourcePaths"/> its paths are written relative to.</param>
    /// <param name="whole">What the application as a whole holds.</param>
    /// <returns>The <see cref="CompilationAnalysis"/>.</returns>
    public static CompilationAnalysis Of(
        Compilation compilation,
        ArtifactCatalog catalog,
        SourcePaths paths,
        WholeApplication whole)
    {
        var diagnostics = whole.Diagnostics;
        var readers = ArtifactReaders.For(compilation, catalog, paths, whole);
        var screens = new ScreenReader(
            whole.Files,
            paths,
            new(diagnostics),
            new(whole.Files, diagnostics, whole.Elsewhere),
            whole.Elsewhere);

        var recovered = new RecoveredArtifacts();
        var reader = new SliceReader(readers, diagnostics, screens, recovered);

        var slices = catalog.Namespaces
            .Select(@namespace => reader.Read(@namespace, catalog.In(@namespace)))
            .OfType<SliceModel>()
            .ToList();

        return new(compilation, catalog, readers, recovered, slices);
    }

    /// <summary>
    /// Attaches the rules the validators of this project declare to the concepts they validate.
    /// </summary>
    /// <remarks>
    /// This waits until every project has been read, because the concept a validator here declares rules for may only
    /// have been reached by an artifact in a project read after this one.
    /// </remarks>
    public void LinkConceptValidations() => ConceptValidations.Link(_catalog, _readers);

    /// <summary>
    /// Reports the errors this compilation carries, at the severity what was recovered from it earns.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>True when the source of this project did not compile.</returns>
    /// <remarks>
    /// Each project answers for its own source. A project of an application that does not build says nothing about
    /// the projects that do, and how much survived the errors is a count of what came out of this compilation.
    /// </remarks>
    public bool ReportCompilationErrors(ScreenplayDiagnostics diagnostics) =>
        CompilationErrors.Report(Compilation, _recovered, diagnostics);
}
