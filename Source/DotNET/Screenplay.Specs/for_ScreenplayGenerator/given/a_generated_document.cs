// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    protected ScreenplayGenerationResult Result;
    protected CompilationResult<SemanticCompilation> Bound;
    protected RoundTripResult RoundTrip;
    protected IUserInterfaceFiles Files = DeclaredUserInterfaceFiles.None;
    Compilation _compilation;

    protected void Generate(params (string Path, string Text)[] sources) => Generate(new ScreenplayOptions(), sources);

    protected void Generate(ScreenplayOptions options, params (string Path, string Text)[] sources)
    {
        _compilation = Analyzed.Compile(sources);
        Result = new ScreenplayGenerator(new ApplicationModelAnalyzer(Files), new ScreenplayEmitter())
            .Generate(_compilation, options);
        RoundTrip = Verification.RoundTrip.For(new ScreenplayCompiler().Compile(Result.Source).Value!);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        const string Key = "application";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(Key), Key, "application.play", Result.Source);
        Bound = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([document], catalog));
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
