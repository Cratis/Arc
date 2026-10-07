// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Library;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// The whole library remains valid authoring syntax even where executable binding is not supported.
/// Syntax verification stays quiet while semantic verification reports those limitations separately.
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
    [Fact] void should_report_only_binding_defects() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().ShouldEqual([ScreenplayDiagnosticCodes.DocumentDidNotBind]);
    [Fact] void should_report_each_binding_defect() => _result.Diagnostics.Count.ShouldEqual(7);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
