// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Screens;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Holds a generated document to the compiler, printer, and executable semantic binder.
/// </summary>
public class a_generated_document : Specification
{
    static readonly ConcurrentDictionary<(ScreenplayOptions Options, string Sources), Lazy<Generated>> _generated = new();

    protected ScreenplayGenerationResult Result;
    protected CompilationResult<SemanticCompilation> Bound;
    protected RoundTripResult RoundTrip;
    protected IUserInterfaceFiles Files = DeclaredUserInterfaceFiles.None;
    Compilation _compilation;

    protected void Generate(params (string Path, string Text)[] sources) => Generate(new ScreenplayOptions(), sources);

    /// <summary>
    /// Generates and binds a document from the given sources.
    /// </summary>
    /// <param name="options">The <see cref="ScreenplayOptions"/> to generate with.</param>
    /// <param name="sources">The C# sources to analyze.</param>
    /// <remarks>
    /// Every fact of a specification class repeats its Establish and Because, so the same pipeline would run once per
    /// fact. Generation is deterministic over the options and sources, and every stage produces immutable results, so
    /// documents generated without declared user interface files are shared across facts instead of regenerated.
    /// </remarks>
    protected void Generate(ScreenplayOptions options, params (string Path, string Text)[] sources)
    {
        var files = Files;
        var generated = ReferenceEquals(files, DeclaredUserInterfaceFiles.None)
            ? _generated.GetOrAdd((options, Key(sources)), static (_, input) => new Lazy<Generated>(() => GenerateDocument(input.Files, input.Options, input.Sources)), (Files: files, Options: options, Sources: sources)).Value
            : GenerateDocument(files, options, sources);
        _compilation = generated.Compilation;
        Result = generated.Result;
        RoundTrip = generated.RoundTrip;
        Bound = generated.Bound;
    }

    /// <summary>
    /// Runs one generated specification through the Screenplay reference runner.
    /// </summary>
    /// <param name="name">The name of the specification.</param>
    /// <returns>The <see cref="SemanticSpecificationRun"/>.</returns>
    protected SemanticSpecificationRun Run(string name)
    {
        var plan = SemanticExecutionPlan.Compile(Bound.Value!.Model).Plan!;
        return new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single(specification => specification.Name == name).Id);
    }

    protected void AssertPartialDocument()
    {
        Analyzed.ErrorsIn(_compilation).ShouldBeEmpty();
        RoundTrip.Errors.ShouldBeEmpty();
        RoundTrip.IsStable.ShouldBeTrue();
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeEmpty();
        Bound.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error &&
            !ExpectedBindingDiagnostics.IsExpected(diagnostic, authoringOnlyConstructs: false)).Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}").ShouldBeEmpty();
    }

    static string Key((string Path, string Text)[] sources) =>
        string.Join('\0', sources.Select(source => $"{source.Path}\u0001{source.Text}"));

    static Generated GenerateDocument(IUserInterfaceFiles files, ScreenplayOptions options, (string Path, string Text)[] sources)
    {
        var compilation = Analyzed.Compile(sources);
        var result = new ScreenplayGenerator(new ApplicationModelAnalyzer(files), new ScreenplayEmitter())
            .Generate(compilation, options);
        var roundTrip = Verification.RoundTrip.For(new ScreenplayCompiler().Compile(result.Source).Value!);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        const string DocumentKey = "application";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(DocumentKey), DocumentKey, "application.play", result.Source);
        var bound = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([document], catalog));
        return new(compilation, result, roundTrip, bound);
    }

    sealed record Generated(
        Compilation Compilation,
        ScreenplayGenerationResult Result,
        RoundTripResult RoundTrip,
        CompilationResult<SemanticCompilation> Bound);

    protected void AssertDocument()
    {
        var errors = Analyzed.ErrorsIn(_compilation).ToArray();
        Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors));
        RoundTrip.Errors.ShouldBeEmpty();
        RoundTrip.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning).ShouldBeEmpty();
        RoundTrip.IsStable.ShouldBeTrue();
        Assert.False(Bound.Diagnostics.Any(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error), string.Join(Environment.NewLine, Bound.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        Bound.Success.ShouldBeTrue();
    }
}
