// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Library;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Reading every generated document back is only worth doing if it stays quiet about the documents that are fine.
/// A check that cries wolf on a real application is a check people learn to ignore, so this generates the whole
/// library - every slice kind, every construct the language holds - and expects nothing said about it.
/// </summary>
public class and_the_document_it_printed_compiles : given.a_compilation
{
    ScreenplayGenerator _generator;
    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;

    void Establish() => _generator = new(new given.a_recovered_model(LibraryApplication.Build()), new ScreenplayEmitter());

    void Because()
    {
        _result = _generator.Generate(_compilation, new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
    }

    [Fact] void should_be_generating_a_document_that_really_does_compile() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_be_generating_the_whole_application() => _result.Source.Contains("slice StateChange Registration", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_report_that_the_document_did_not_compile() => _result.Diagnostics.Select(_ => _.Code).ShouldNotContain(ScreenplayDiagnosticCodes.DocumentDidNotCompile);
    [Fact] void should_report_nothing_at_all() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
