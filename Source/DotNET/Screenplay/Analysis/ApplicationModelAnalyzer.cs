// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Analysis.Policies;
using Cratis.Arc.Screenplay.Analysis.Screens;
using Cratis.Arc.Screenplay.Analysis.Slices;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Represents an <see cref="IApplicationModelAnalyzer"/> that recovers the model from the source of an application.
/// </summary>
/// <param name="userInterfaceFiles">The <see cref="IUserInterfaceFiles"/> the screens of a slice are found through.</param>
/// <remarks>
/// Projects are read in a fixed order, namespaces within a project are read in order and everything within a slice is
/// ordered explicitly, so the same source always yields the same model whichever order its projects were handed over
/// in - which is what makes the document it produces something worth committing.
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
    public ApplicationModelAnalysis Analyze(Compilation compilation, ScreenplayOptions options) =>
        Analyze([compilation], options);

    /// <inheritdoc/>
    public ApplicationModelAnalysis Analyze(IReadOnlyList<Compilation> compilations, ScreenplayOptions options)
    {
        var ordered = AnalyzedCompilations.Ordered(compilations);
        var domain = options.Domain ?? AnalyzedCompilations.NameOf(ordered) ?? ScreenplayOptions.DefaultName;
        var whole = new WholeApplication(ordered, new ScreenplayDiagnostics()) { Files = userInterfaceFiles };
        var diagnostics = whole.Diagnostics;

        var catalogs = ordered.Select(ArtifactCatalog.From).ToList();
        var paths = SourceRoots.Across(ordered, catalogs, diagnostics, domain);
        var projects = ordered
            .Select((compilation, index) => CompilationAnalysis.Of(compilation, catalogs[index], paths[index], whole))
            .ToList();

        foreach (var project in projects)
        {
            project.LinkConceptValidations();
        }

        whole.AggregateRoots.Report(diagnostics);
        whole.Elsewhere.Report(diagnostics);
        TypesTheDocumentCannotName.Report(whole.Types, diagnostics, domain);

        var slices = SliceUnion.Of(projects.SelectMany(_ => _.Slices), diagnostics);
        var imports = ExternalEvents.Resolve(ordered, slices, diagnostics);
        NamespacesWithoutStructure.Report(slices, diagnostics, options.SegmentsToSkip ?? 0);

        // How serious source that did not compile is depends on how much survived it, so it is reported once the
        // slices are in rather than on the way in. Source that did not compile still suppresses "declares nothing" -
        // an empty document from a broken build is the broken build, not an application with nothing in it - and
        // when it is reported as a warning the suppression can never apply, because a warning is only ever reached
        // when something was recovered and something recovered is a slice. Each project answers for its own source,
        // so a build broken in one of them says nothing about the ones that built.
        var failedToCompile = false;
        foreach (var project in projects)
        {
            failedToCompile |= project.ReportCompilationErrors(diagnostics);
        }

        if (slices.Count == 0 && !failedToCompile)
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.AnalysisUnavailable,
                "The source declares nothing that can be expressed, so the generated document describes nothing",
                domain);
        }

        return new(
            new ApplicationModel(
                domain,
                options.Module ?? options.Domain ?? ScreenplayOptions.DefaultName,
                whole.Types.Concepts,
                new PolicyCatalog(ordered, domain, diagnostics).Declare(slices.SelectMany(AuthorizationsIn)),
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
}
