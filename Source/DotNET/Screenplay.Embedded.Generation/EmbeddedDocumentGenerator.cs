// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Emission.Validation;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Embedded.Generation;

/// <summary>
/// Represents an implementation of <see cref="IEmbeddedDocumentGenerator"/>.
/// </summary>
/// <param name="analyzer">The <see cref="IApplicationModelAnalyzer"/> recovering the model from the source.</param>
/// <param name="emitter">The <see cref="IScreenplayEmitter"/> turning each scope of the model into a document.</param>
/// <param name="verifier">The <see cref="IScreenplayVerifier"/> every generated document is read back with.</param>
/// <remarks>
/// The three halves of the existing generator are reused as they are. The source is analyzed once - analyzing it
/// per document would be the same work repeated and could recover different models for the same application - and
/// every document is emitted from a narrowed view of that one model and then read back with the compiler the
/// language ships. Nothing is embedded that the Screenplay compiler has not accepted.
/// </remarks>
public class EmbeddedDocumentGenerator(
    IApplicationModelAnalyzer analyzer,
    IScreenplayEmitter emitter,
    IScreenplayVerifier verifier) : IEmbeddedDocumentGenerator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddedDocumentGenerator"/> class with everything wired up.
    /// </summary>
    public EmbeddedDocumentGenerator()
        : this(new ApplicationModelAnalyzer(), new ScreenplayEmitter(), new ScreenplayVerifier())
    {
    }

    /// <inheritdoc/>
    public EmbeddedDocumentGeneration Generate(Compilation compilation, EmbeddedDocumentOptions options) =>
        Generate([compilation], options);

    /// <inheritdoc/>
    public EmbeddedDocumentGeneration Generate(IReadOnlyList<Compilation> compilations, EmbeddedDocumentOptions options)
    {
        ArgumentNullException.ThrowIfNull(compilations);

        var resolved = options.Resolve();
        var analysis = analyzer.Analyze(compilations, ScreenplayOptionsFor(DocumentScopes.Root(resolved), resolved));

        return Generate(analysis.Model, resolved, analysis.Diagnostics);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A diagnostic about the model itself - a slice that declares nothing, a projection that cannot be expressed -
    /// is reported once however many documents hold that part of the application. Repeating it per document would
    /// say the same defect exists several times, which is the opposite of what the reader needs from a build log.
    /// A diagnostic naming a document, such as one the Screenplay compiler rejected, names its own scope and so
    /// stays distinct.
    /// </remarks>
    public EmbeddedDocumentGeneration Generate(
        ApplicationModel model,
        EmbeddedDocumentOptions options,
        IEnumerable<ScreenplayDiagnostic> analyzed)
    {
        var resolved = options.Resolve();
        var reported = analyzed as IReadOnlyList<ScreenplayDiagnostic> ?? [.. analyzed];
        var diagnostics = new ScreenplayDiagnostics();
        diagnostics.AddRange(reported);

        var documents = new List<GeneratedDocument>();
        var verify = !SourceDidNotCompile(reported);

        // Concepts belong to the whole application, even in documents without their exercised carrier commands.
        model = new ExecutableValidationRules(diagnostics).Apply(model);
        var scopes = DocumentScopes.Of(model, resolved);
        foreach (var scope in scopes)
        {
            var emission = emitter.Emit(ScopedApplicationModel.For(model, scope), ScreenplayOptionsFor(scope, resolved));
            diagnostics.AddRange(emission.Diagnostics);

            if (verify)
            {
                ReportDocumentThatDoesNotCompile(emission.Source, scope, diagnostics, resolved.AuthoringOnlyConstructs);
            }

            if (scope.Kind == EmbeddedDocumentKind.Assembly && emission.Application is not null)
            {
                var arranged = AssemblyDocumentArrangement.Apply(emission, scopes);
                if (verify && !string.Equals(emission.Source, arranged.Source, StringComparison.Ordinal))
                {
                    ReportDocumentThatDoesNotCompile(arranged.Source, scope, diagnostics, resolved.AuthoringOnlyConstructs);
                }

                emission = arranged;
            }

            documents.Add(new(scope.Document, emission.Source));
        }

        return new(documents, [.. diagnostics.All.Distinct()]);
    }

    /// <summary>
    /// Gets the options one scope of the model is emitted with.
    /// </summary>
    /// <param name="scope">The scope of the document.</param>
    /// <param name="options">The resolved options the documents are generated with.</param>
    /// <returns>The resolved <see cref="ScreenplayOptions"/>.</returns>
    static ScreenplayOptions ScreenplayOptionsFor(DocumentScope scope, EmbeddedDocumentOptions options) =>
        new ScreenplayOptions
        {
            Domain = options.AssemblyName,
            Module = scope.ModuleName,
            SegmentsToSkip = scope.SegmentsToSkip,
            AuthoringOnlyConstructs = options.AuthoringOnlyConstructs
        }.WithDefaults(options.AssemblyName);

    /// <summary>
    /// Gets a value indicating whether the source the model was recovered from did not compile.
    /// </summary>
    /// <param name="diagnostics">What recovering the model reported.</param>
    /// <returns>True when nothing recovered can be trusted, false otherwise.</returns>
    /// <remarks>
    /// A model recovered from symbols the C# compiler never accepted describes an application that does not exist,
    /// so a document the Screenplay compiler rejects is the consequence already reported rather than a second
    /// defect - the same bargain the existing generator strikes, and for the same reason.
    /// </remarks>
    static bool SourceDidNotCompile(IEnumerable<ScreenplayDiagnostic> diagnostics) =>
        diagnostics.Any(_ =>
            _.Code == ScreenplayDiagnosticCodes.SourceDidNotCompile &&
            _.Severity == ScreenplayDiagnosticSeverity.Error);

    /// <summary>
    /// Reads a printed document back and reports syntax and unexpected semantic binding errors.
    /// </summary>
    /// <param name="source">The printed document.</param>
    /// <param name="scope">The scope the document describes.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <param name="authoringOnlyConstructs">Whether additional authoring-only constructs were emitted.</param>
    /// <remarks>
    /// Embedding a document nobody can open is worse than failing the build, because the failure then surfaces in
    /// an application rather than in the build that produced it. Every document is read back, including the ones
    /// scoping narrows - a reference that resolved while the whole application was in one document is exactly the
    /// kind of thing narrowing breaks.
    /// </remarks>
    void ReportDocumentThatDoesNotCompile(string source, DocumentScope scope, ScreenplayDiagnostics diagnostics, bool authoringOnlyConstructs)
    {
        var verification = verifier.Verify(source);

        if (verification.Compiles)
        {
            foreach (var error in verification.UnexpectedBindingErrors(authoringOnlyConstructs))
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.DocumentDidNotBind,
                    $"The generated document for '{scope.Id}' did not bind - {error.Code}: '{error.Message}' on line {error.Location.Line}, column {error.Location.Column}. That is the generator being wrong rather than anything the source declared, and the document is returned as it stands so the line can be read",
                    scope.Namespace);
            }

            return;
        }

        var first = verification.Errors[0];

        diagnostics.Error(
            ScreenplayDiagnosticCodes.DocumentDidNotCompile,
            $"The generated document for '{scope.Id}' did not compile - {verification.Errors.Count} error(s), the first being '{first.Message}' on line {first.Location.Line}. That is the generator being wrong rather than anything the source declared, and nothing is embedded that the Screenplay compiler has not accepted",
            scope.Namespace);
    }
}
