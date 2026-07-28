// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Verification;
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
    public ScreenplayGenerationResult Generate(Compilation compilation, ScreenplayOptions options)
    {
        var resolved = options.WithDefaults(compilation.AssemblyName);
        var analysis = analyzer.Analyze(compilation, resolved);
        var emission = emitter.Emit(analysis.Model, resolved);

        var diagnostics = new ScreenplayDiagnostics();
        diagnostics.AddRange(analysis.Diagnostics);
        diagnostics.AddRange(emission.Diagnostics);
        ReportDocumentThatDoesNotCompile(emission.Source, analysis.Diagnostics, diagnostics, compilation.AssemblyName);

        return new(emission.Source, analysis.Model, diagnostics.All);
    }

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
    /// </remarks>
    void ReportDocumentThatDoesNotCompile(
        string source,
        IEnumerable<ScreenplayDiagnostic> analyzed,
        ScreenplayDiagnostics diagnostics,
        string? location)
    {
        if (analyzed.Any(_ => _.Code == ScreenplayDiagnosticCodes.SourceDidNotCompile))
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
