// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class an_authoring_document : Specification
{
    protected ScreenplayGenerationResult Result;
    protected ScreenplayGenerationResult Off;
    protected CompilationResult<SemanticCompilation> Bound;
    protected CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> Compiled;
    protected RoundTripResult RoundTrip;
    Compilation _compilation;

    protected void Generate(string source)
    {
        _compilation = Analyzed.Compile((Analyzed.SlicePath, source));
        var generator = new ScreenplayGenerator();
        Result = generator.Generate(_compilation, new ScreenplayOptions { AuthoringOnlyConstructs = true });
        Off = generator.Generate(_compilation, new ScreenplayOptions());
        generator.Generate(_compilation, new ScreenplayOptions { AuthoringOnlyConstructs = false }).Source.ShouldEqual(Off.Source);
        Compiled = new ScreenplayCompiler().Compile(Result.Source);
        if (Compiled.Value is not null)
        {
            RoundTrip = Verification.RoundTrip.For(Compiled.Value);
        }

        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        const string Key = "application";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(Key), Key, "application.play", Result.Source);
        Bound = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([document], catalog));
    }

    protected void AssertAuthoringDocument(bool legacyReads = false)
    {
        Analyzed.ErrorsIn(_compilation).ShouldBeEmpty();
        Compiled.Diagnostics.Where(diagnostic => diagnostic.Severity is Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error or Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning)
            .Select(diagnostic => diagnostic.Message).ShouldBeEmpty();
        RoundTrip.Errors.ShouldBeEmpty();
        RoundTrip.IsStable.ShouldBeTrue();
        Bound.Success.ShouldBeFalse();
        Bound.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error).ShouldNotBeEmpty();
        var unexpected = Bound.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error && diagnostic.Code != "PLAY0268" && (!legacyReads || diagnostic.Code != "PLAY0271")).ToArray();
        Assert.True(unexpected.Length == 0, string.Join(Environment.NewLine, unexpected.Select(diagnostic => diagnostic.Code + ": " + diagnostic.Message)));
    }
}
