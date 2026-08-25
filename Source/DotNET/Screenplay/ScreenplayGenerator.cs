// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Represents an implementation of <see cref="IScreenplayGenerator"/>.
/// </summary>
/// <param name="analyzer">The <see cref="IApplicationModelAnalyzer"/> recovering the model from the source.</param>
/// <param name="emitter">The <see cref="IScreenplayEmitter"/> turning the model into a document.</param>
/// <remarks>
/// The package ships no dependency injection of its own, so a consumer that just wants to generate a document says
/// <c>new ScreenplayGenerator()</c> and gets everything wired. The constructor taking collaborators exists for
/// specifications and for hosts that want to substitute one half. Reading the generated document back is not one of
/// the halves - a host that could substitute it could switch it off, and a check nobody runs is the situation this
/// exists to end.
/// </remarks>
public class ScreenplayGenerator(IApplicationModelAnalyzer analyzer, IScreenplayEmitter emitter) : IScreenplayGenerator
{
    readonly ScreenplayVerifier _verifier = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenplayGenerator"/> class with everything wired up.
    /// </summary>
    /// <remarks>
    /// This is the single place the default halves are chosen, so a host that substitutes one of them changes
    /// nothing else.
    /// </remarks>
    public ScreenplayGenerator()
        : this(new ApplicationModelAnalyzer(), new ScreenplayEmitter())
    {
    }

    /// <inheritdoc/>
    public ScreenplayGenerationResult Generate(Compilation compilation, ScreenplayOptions options) =>
        Generate([compilation], options);

    /// <inheritdoc/>
    public ScreenplayGenerationResult Generate(DotNetProjectCompilation project, ScreenplayOptions options) =>
        Generate(project.Compilation, options);

    /// <inheritdoc/>
    /// <remarks>
    /// A generation is the entry point that knows what a name falls back to - the assembly being read, or nothing at
    /// all when several projects are the application together - so the options resolve here and both halves run with
    /// the same answer. Emitting resolves too, because a host can emit a model without ever generating, but options
    /// that are resolved answer with themselves and nothing is resolved twice on the way through.
    /// </remarks>
    public ScreenplayGenerationResult Generate(IReadOnlyList<Compilation> compilations, ScreenplayOptions options)
    {
        var ordered = AnalyzedCompilations.Ordered(compilations);
        var resolved = options.WithDefaults(AnalyzedCompilations.NameOf(ordered));
        var analysis = analyzer.Analyze(ordered, resolved);
        var emission = emitter.Emit(analysis.Model, resolved);

        var diagnostics = new ScreenplayDiagnostics();
        diagnostics.AddRange(analysis.Diagnostics);
        diagnostics.AddRange(emission.Diagnostics);
        ReportDocumentThatDoesNotCompile(emission.Source, analysis.Diagnostics, diagnostics, resolved.Domain);

        return new(emission.Source, analysis.Model, diagnostics.All);
    }

    /// <inheritdoc/>
    public ScreenplayGenerationResult Generate(IReadOnlyList<DotNetProjectCompilation> projects, ScreenplayOptions options) =>
        Generate([.. projects.Select(_ => _.Compilation)], options);

    /// <summary>
    /// Reads the printed document back and reports one the Screenplay compiler rejects.
    /// </summary>
    /// <param name="source">The printed document.</param>
    /// <param name="analyzed">What analysis reported, which says whether the source it read compiled.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <param name="location">Where to report against.</param>
    /// <remarks>
    /// Everything else reported names something the application declared that the language cannot hold. This names
    /// the generator being wrong, which is why it runs on every generation rather than on request - the only way a
    /// rejected document is ever found is by reading each one back, and a document nobody happened to look at is
    /// exactly how one shipped. Reporting it as an error is what makes a host exit non zero, and the text is
    /// returned regardless so the line that was rejected can be read - the same bargain
    /// <see cref="ScreenplayDiagnosticCodes.SourceDidNotCompile"/> strikes. That code also suppresses this one, the
    /// way it suppresses <see cref="ScreenplayDiagnosticCodes.AnalysisUnavailable"/>: a model recovered from symbols
    /// the compiler never accepted describes an application that does not exist, so a poor document made from it is
    /// the consequence already reported rather than a second defect.
    /// <para>
    /// The suppression follows the severity that code was reported at rather than the code alone. As an error it
    /// says nothing recovered can be trusted, which is the whole reason a document built from it says nothing
    /// either. As a warning it says the opposite - that what was recovered stands - and a document built from a
    /// model that stands is exactly what this check is for. Suppressing it there would hand back a document the
    /// language rejects with nothing wrong reported, which is the one outcome this exists to make impossible.
    /// </para>
    /// </remarks>
    void ReportDocumentThatDoesNotCompile(
        string source,
        IEnumerable<ScreenplayDiagnostic> analyzed,
        ScreenplayDiagnostics diagnostics,
        string? location)
    {
        if (analyzed.Any(_ => _.Code == ScreenplayDiagnosticCodes.SourceDidNotCompile && _.Severity == ScreenplayDiagnosticSeverity.Error))
        {
            return;
        }

        var verification = _verifier.Verify(source);
        if (verification.Compiles)
        {
            return;
        }

        var first = verification.Errors[0];

        diagnostics.Error(
            ScreenplayDiagnosticCodes.DocumentDidNotCompile,
            $"The generated document did not compile - {verification.Errors.Count} error(s), the first being '{first.Message}' on line {first.Location.Line}. That is the generator being wrong rather than anything the source declared, and the document is returned as it stands so the line can be read",
            location);
    }
}
