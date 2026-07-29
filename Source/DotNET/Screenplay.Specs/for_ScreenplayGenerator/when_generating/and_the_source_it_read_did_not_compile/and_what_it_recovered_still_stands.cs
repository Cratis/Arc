// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.and_the_source_it_read_did_not_compile;

/// <summary>
/// This is the point of the whole thing. A host that follows the documented contract discards the document on
/// anything reported as an error, so a compilation that lost only its generated symbols used to cost the caller a
/// complete and correct document. The errors are still reported - nothing is hidden - but the result is successful,
/// because what was recovered is described exactly as the source states it.
/// </summary>
public class and_what_it_recovered_still_stands : Specification
{
    Compilation _compilation;
    ScreenplayGenerator _generator;
    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;

    void Establish()
    {
        _compilation = Analyzed.Compile(RecoveringSource.Files());
        _generator = new();
    }

    void Because()
    {
        _result = _generator.Generate(_compilation, new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
    }

    ScreenplayDiagnostic Reported => _result.Diagnostics.First(_ => _.Code == ScreenplayDiagnosticCodes.SourceDidNotCompile);

    [Fact] void should_be_generating_from_source_that_really_does_not_compile() => Analyzed.ErrorsIn(RecoveringSource.Files()).ShouldNotBeEmpty();
    [Fact] void should_report_that_the_source_did_not_compile() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.SourceDidNotCompile);
    [Fact] void should_report_it_as_a_warning() => Reported.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_nothing_at_all_as_an_error() => _result.Diagnostics.Any(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeFalse();
    [Fact] void should_describe_the_command_the_source_declares() => _result.Source.Contains("command RegisterAuthor", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_describe_the_event_the_source_declares() => _result.Source.Contains("event AuthorRegistered", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
}
