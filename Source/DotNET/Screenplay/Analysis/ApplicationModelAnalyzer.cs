// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        var failedToCompile = ReportCompilationErrors(compilation, diagnostics);
        var catalog = ArtifactCatalog.From(compilation);
        var readers = ArtifactReaders.For(compilation, catalog, diagnostics);
        var screens = new ScreenReader(userInterfaceFiles, SourcePaths.For(compilation), new(diagnostics));
        var reader = new SliceReader(readers, diagnostics, screens);

        var slices = catalog.Namespaces
            .Select(@namespace => reader.Read(@namespace, catalog.In(@namespace)))
            .OfType<SliceModel>()
            .ToList();

        ConceptValidations.Link(catalog, readers);
        readers.AggregateRoots.Report(diagnostics);
        ReportEventsFromOutside(slices, diagnostics);
        ReportNamespacesWithoutStructure(slices, diagnostics, options.SegmentsToSkip ?? 0);

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
                slices),
            diagnostics.All);
    }

    /// <summary>
    /// Reports source that did not compile, which nothing recovered from it can be relied on past.
    /// </summary>
    /// <param name="compilation">The compilation to check.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>True when the source did not compile.</returns>
    /// <remarks>
    /// A compilation that does not build still yields symbols, and analyzing them produces a document that looks
    /// like an answer while describing an application that does not exist. Reporting it as an error rather than a
    /// warning is what makes a host exit non zero, because a nearly empty document and a success code is the one
    /// outcome nobody can act on. The model is still returned - what was recovered is worth seeing, as long as
    /// nobody is told it is trustworthy.
    /// </remarks>
    static bool ReportCompilationErrors(Compilation compilation, ScreenplayDiagnostics diagnostics)
    {
        var errors = compilation.GetDiagnostics().Where(_ => _.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count == 0)
        {
            return false;
        }

        diagnostics.Error(
            ScreenplayDiagnosticCodes.SourceDidNotCompile,
            $"The source did not compile - {errors.Count} error(s), the first being '{errors[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture)}'. Nothing recovered from it describes the application reliably",
            compilation.AssemblyName);

        return true;
    }

    /// <summary>
    /// Gets everything within a slice that requires something of the caller.
    /// </summary>
    /// <param name="slice">The slice to read.</param>
    /// <returns>The authorizations.</returns>
    static IEnumerable<AuthorizationModel> AuthorizationsIn(SliceModel slice) =>
        slice.Commands.Select(_ => _.Authorization).Concat(slice.Queries.Select(_ => _.Authorization)).OfType<AuthorizationModel>();

    /// <summary>
    /// Reports every event the application refers to but does not declare.
    /// </summary>
    /// <param name="slices">The slices to check.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <remarks>
    /// An event living in a referenced package is real, but nothing in the compilation declares it, so the document
    /// would refer to something it never introduces. Saying so is better than inventing a declaration for it.
    /// </remarks>
    static void ReportEventsFromOutside(IReadOnlyList<SliceModel> slices, ScreenplayDiagnostics diagnostics)
    {
        var declared = slices.SelectMany(_ => _.Events).Select(_ => _.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var slice in slices)
        {
            foreach (var name in ReferencedEvents(slice).Where(_ => !declared.Contains(_)).Order(StringComparer.Ordinal))
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.EventDeclaredOutsideCompilation,
                    $"'{name}' is referred to but declared outside the compilation, so the document refers to an event it never introduces",
                    slice.Namespace);
            }
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

    /// <summary>
    /// Gets the names of every event a slice refers to.
    /// </summary>
    /// <param name="slice">The slice to read.</param>
    /// <returns>The names, distinct.</returns>
    static IEnumerable<string> ReferencedEvents(SliceModel slice) =>
        slice.Commands.SelectMany(_ => _.Produces).Select(_ => _.EventName)
            .Concat(slice.Reactors.SelectMany(_ => _.ObservedEvents))
            .Concat(slice.Constraints.SelectMany(EventsOf))
            .Concat(ProjectionEvents.In(slice.Projection))
            .Distinct(StringComparer.Ordinal);

    /// <summary>
    /// Gets the names of the events a constraint refers to.
    /// </summary>
    /// <param name="constraint">The constraint to read.</param>
    /// <returns>The names.</returns>
    static IEnumerable<string> EventsOf(ConstraintModel constraint) => constraint switch
    {
        UniquePropertyConstraintModel unique => [unique.EventName],
        UniqueEventConstraintModel unique => [unique.EventName],
        _ => []
    };
}
