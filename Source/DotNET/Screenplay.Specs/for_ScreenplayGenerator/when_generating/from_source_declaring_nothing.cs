// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Source that declares no artifact leaves nothing to describe. The generator still has to produce a document that
/// compiles, and it has to say that it recovered nothing rather than imply the application is empty.
/// </summary>
public class from_source_declaring_nothing : given.a_compilation
{
    ScreenplayGenerator _generator;
    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;

    void Establish() => _generator = new(new ApplicationModelAnalyzer(), new ScreenplayEmitter());

    void Because()
    {
        _result = _generator.Generate(_compilation, new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
    }

    [Fact] void should_still_name_the_domain() => _result.Source.ShouldEqual("domain Library\n");
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_produce_a_document_without_diagnostics() => _compiled.Diagnostics.ShouldBeEmpty();
    [Fact] void should_report_that_nothing_was_recovered() => _result.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.AnalysisUnavailable]);
    [Fact] void should_not_report_it_as_a_failure() => _result.IsSuccess.ShouldBeTrue();
}
