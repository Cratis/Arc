// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Analysis.Policies;
using Cratis.Arc.Screenplay.Analysis.Screens;
using Cratis.Arc.Screenplay.Analysis.Slices;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Represents an <see cref="IApplicationModelAnalyzer"/> that recovers the model from the source of an application.
/// </summary>
/// <param name="userInterfaceFiles">The <see cref="IUserInterfaceFiles"/> the screens of a slice are found through.</param>
/// <remarks>
/// Namespaces are read in order and everything within a slice is ordered explicitly, so the same compilation always
/// yields the same model - which is what makes the document it produces something worth committing.
/// </remarks>
public class ApplicationModelAnalyzer(IUserInterfaceFiles userInterfaceFiles) : IApplicationModelAnalyzer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationModelAnalyzer"/> class reading the file system the
    /// source was built from.
    /// </summary>
    /// <remarks>
    /// A compilation answers for everything but the file realizing a screen, which is why that one question is
    /// asked of a collaborator. This is the single place the answer comes from a real disk, so a host that wants a
    /// compilation analyzed without one substitutes it and everything else stays as it is.
    /// </remarks>
    public ApplicationModelAnalyzer()
        : this(new UserInterfaceFiles())
    {
    }

    /// <inheritdoc/>
    public ApplicationModelAnalysis Analyze(Compilation compilation, ScreenplayOptions options)
    {
        var diagnostics = new ScreenplayDiagnostics();
        var catalog = ArtifactCatalog.From(compilation);
        var readers = ArtifactReaders.For(compilation, catalog, diagnostics);
        var elsewhere = new CrossSliceQueries();
        var screens = new ScreenReader(
            userInterfaceFiles,
            SourcePaths.For(compilation, catalog),
            new(diagnostics),
            new(userInterfaceFiles, diagnostics, elsewhere),
            elsewhere);

        var recovered = new RecoveredArtifacts();
        var reader = new SliceReader(readers, diagnostics, screens, recovered);

        var slices = catalog.Namespaces
            .Select(@namespace => reader.Read(@namespace, catalog.In(@namespace)))
            .OfType<SliceModel>()
            .ToList();

        ConceptValidations.Link(catalog, readers);
        readers.AggregateRoots.Report(diagnostics);
        elsewhere.Report(diagnostics);
        ReportTypesTheDocumentCannotName(readers, diagnostics, compilation.AssemblyName);
        var imports = ExternalEvents.Resolve(compilation, slices, diagnostics);
        ReportNamespacesWithoutStructure(slices, diagnostics, options.SegmentsToSkip ?? 0);

        // How serious source that did not compile is depends on how much survived it, so it is reported once the
        // slices are in rather than on the way in. Source that did not compile still suppresses "declares nothing" -
        // an empty document from a broken build is the broken build, not an application with nothing in it - and
        // when it is reported as a warning the suppression can never apply, because a warning is only ever reached
        // when something was recovered and something recovered is a slice.
        var failedToCompile = CompilationErrors.Report(compilation, recovered, diagnostics);

        if (slices.Count == 0 && !failedToCompile)
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.AnalysisUnavailable,
                "The source declares nothing that can be expressed, so the generated document describes nothing",
                compilation.AssemblyName);
        }

        return new(
            new ApplicationModel(
                options.Domain ?? compilation.AssemblyName ?? ScreenplayOptions.DefaultName,
                options.Module ?? options.Domain ?? ScreenplayOptions.DefaultName,
                readers.Types.Concepts,
                new PolicyCatalog(compilation, diagnostics).Declare(slices.SelectMany(AuthorizationsIn)),
                slices)
            {
                Imports = imports
            },
            diagnostics.All);
    }

    /// <summary>
    /// Gets everything within a slice that requires something of the caller.
    /// </summary>
    /// <param name="slice">The slice to read.</param>
    /// <returns>The authorizations.</returns>
    static IEnumerable<AuthorizationModel> AuthorizationsIn(SliceModel slice) =>
        slice.Commands.Select(_ => _.Authorization).Concat(slice.Queries.Select(_ => _.Authorization)).OfType<AuthorizationModel>();

    /// <summary>
    /// Reports every type the document had to refer to by a name that does not say what it is.
    /// </summary>
    /// <param name="readers">The readers holding the types encountered.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <param name="location">Where to report against.</param>
    /// <remarks>
    /// A Screenplay type reference is a single identifier, so a type the grammar cannot hold is written as whatever
    /// of its name survives - and the property then claims a type nothing declares. Saying which types those were is
    /// the difference between a document with a known gap and a document that is quietly wrong.
    /// </remarks>
    static void ReportTypesTheDocumentCannotName(ArtifactReaders readers, ScreenplayDiagnostics diagnostics, string? location)
    {
        foreach (var type in readers.Types.Unmappable)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableTypeReference,
                $"'{type}' has no Screenplay counterpart, so it is referred to by a name the document never declares",
                location);
        }

        foreach (var type in readers.Types.Ambiguous)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.AmbiguousConceptName,
                $"'{type}' shares its simple name with a concept the document already declares, so what it is is described by the first one instead",
                location);
        }

        foreach (var shape in readers.Types.Shapes)
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.UndeclarableShape,
                $"'{shape}' is a record an artifact carries, and there is no way to declare what a record holds, so the document names it without saying what is in it - the concepts it carries are declared, the shape itself is not",
                location);
        }
    }

    /// <summary>
    /// Reports a namespace that carries nothing to arrange the document by.
    /// </summary>
    /// <param name="slices">The slices to check.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <param name="segmentsToSkip">The number of leading namespace segments being skipped.</param>
    /// <remarks>
    /// Artifacts sitting in the root namespace leave the module, the feature and the slice with nothing to be named
    /// after but the assembly, so all of them end up saying the same word. Naming them anything else would be
    /// fiction - the source really does say nothing about where they belong - so this says what would fix it
    /// instead, which is either a namespace per slice or a leading segment skipped.
    /// </remarks>
    static void ReportNamespacesWithoutStructure(
        IEnumerable<SliceModel> slices,
        ScreenplayDiagnostics diagnostics,
        int segmentsToSkip)
    {
        foreach (var slice in slices.Where(_ => Segments(_.Namespace, segmentsToSkip) <= 1))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.NamespaceWithoutStructure,
                "The namespace carries no feature or slice to arrange by, so the module, the feature and the slice all take the same name - give the slice a namespace of its own, or skip a leading segment",
                slice.Namespace);
        }
    }

    /// <summary>
    /// Counts the namespace segments left to arrange a slice by.
    /// </summary>
    /// <param name="namespace">The namespace to count.</param>
    /// <param name="segmentsToSkip">The number of leading segments being skipped.</param>
    /// <returns>The number of segments.</returns>
    static int Segments(string @namespace, int segmentsToSkip) =>
        @namespace.Split('.', StringSplitOptions.RemoveEmptyEntries).Length - segmentsToSkip;
}
