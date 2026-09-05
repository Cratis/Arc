// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Screenplay;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A document the Screenplay compiler rejects is output nobody can use, and no way of writing an application avoids
/// it - it is the generator being wrong. Nothing but reading every generated document back finds one, which is how
/// a property named after a directive shipped with 690 green specifications behind it. So every generation compiles
/// what it printed, reports a rejected document as an error - which is what makes a host exit non zero - and hands
/// the text back regardless, because the line that was rejected is the only place to start looking.
/// </summary>
public class and_the_document_it_printed_does_not_compile : given.a_document_the_language_rejects
{
    Compilation _compilation;
    ScreenplayGenerator _generator;
    ScreenplayGenerationResult _result;

    void Establish()
    {
        _compilation = Analyzed.Compile(("Library/Authors/Registration/Registration.cs", "namespace Library.Authors.Registration;"));
        _generator = new(new ApplicationModelAnalyzer(), _emitter);
    }

    void Because() => _result = _generator.Generate(_compilation, new ScreenplayOptions());

    ScreenplayDiagnostic Reported => _result.Diagnostics.First(_ => _.Code == ScreenplayDiagnosticCodes.DocumentDidNotCompile);

    [Fact] void should_be_printing_a_document_the_language_really_rejects() => new ScreenplayCompiler().Compile(Rejected).Success.ShouldBeFalse();
    [Fact] void should_report_that_the_document_did_not_compile() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.DocumentDidNotCompile);
    [Fact] void should_report_it_as_an_error() => Reported.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Error);
    [Fact] void should_report_it_only_once() => _result.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.DocumentDidNotCompile).ShouldEqual(1);
    [Fact] void should_quote_the_first_thing_the_compiler_said() => Reported.Message.Contains("description RequestDescription", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_say_which_line_was_rejected() => Reported.Message.Contains($"on line {RejectedLine}", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_say_it_is_the_generator_that_is_wrong() => Reported.Message.Contains("the generator being wrong", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_still_return_the_document() => _result.Source.ShouldEqual(Rejected);
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_claim_the_source_it_read_failed_to_compile() => _result.Diagnostics.Select(_ => _.Code).ShouldNotContain(ScreenplayDiagnosticCodes.SourceDidNotCompile);
}
