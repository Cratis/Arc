// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Screens;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay;
using Cratis.Screenplay.Syntax;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

public class a_generated_document_without_executable_binding : Specification
{
    protected ScreenplayGenerationResult Result;
    protected IUserInterfaceFiles Files = DeclaredUserInterfaceFiles.None;
    Compilation _compilation;
    CompilationResult<ApplicationSyntax> _compiled;
    RoundTripResult _roundTrip;

    protected void Generate(ScreenplayOptions options, params (string Path, string Text)[] sources)
    {
        // ESM v1 rejects repeated query names independently of screen bindings and explicitly defers screens.
        _compilation = Analyzed.Compile(sources);
        Result = new ScreenplayGenerator(new ApplicationModelAnalyzer(Files), new ScreenplayEmitter()).Generate(_compilation, options);
        _compiled = new ScreenplayCompiler().Compile(Result.Source);
        _roundTrip = Verification.RoundTrip.For(_compiled.Value!);
    }

    protected void AssertDocument()
    {
        Analyzed.ErrorsIn(_compilation).ShouldBeEmpty();
        _compiled.Success.ShouldBeTrue();
        _compiled.Diagnostics.WithoutTimelineInformation().ShouldBeEmpty();
        _roundTrip.Errors.ShouldBeEmpty();
        _roundTrip.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning).ShouldBeEmpty();
        _roundTrip.IsStable.ShouldBeTrue();
    }

    protected bool Says(string text) => Result.Source.Contains(text, StringComparison.Ordinal);
}
