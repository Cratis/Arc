// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
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
    Compilation _compilation;

    protected void Generate(params (string Path, string Text)[] sources)
    {
        _compilation = Analyzed.Compile(sources);
        Result = new ScreenplayGenerator(new ApplicationModelAnalyzer(DeclaredUserInterfaceFiles.None), new ScreenplayEmitter())
            .Generate(_compilation, new ScreenplayOptions());
        RoundTrip = Verification.RoundTrip.For(new ScreenplayCompiler().Compile(Result.Source).Value!);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        const string Key = "application";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(Key), Key, "application.play", Result.Source);
        Bound = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([document], catalog));
    }

    protected void AssertDocument()
    {
        var errors = Analyzed.ErrorsIn(_compilation).ToArray();
        Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors));
        RoundTrip.Errors.ShouldBeEmpty();
        RoundTrip.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning).ShouldBeEmpty();
        RoundTrip.IsStable.ShouldBeTrue();
        Bound.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message).ShouldBeEmpty();
        Bound.Success.ShouldBeTrue();
    }
}
