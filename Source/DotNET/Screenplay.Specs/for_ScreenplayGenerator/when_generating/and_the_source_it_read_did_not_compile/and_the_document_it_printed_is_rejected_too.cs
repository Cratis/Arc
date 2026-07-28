// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Screenplay;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.and_the_source_it_read_did_not_compile;

/// <summary>
/// The suppression of the document check follows the severity rather than the code. Source that did not compile but
/// gave up everything it declares says the model stands, and a document built from a model that stands is exactly
/// what the check exists for - suppressing it here would hand back a document the language rejects with nothing
/// wrong reported, which is the one outcome nobody can act on.
/// </summary>
public class and_the_document_it_printed_is_rejected_too : given.a_document_the_language_rejects
{
    Compilation _compilation;
    ScreenplayGenerator _generator;
    ScreenplayGenerationResult _result;

    void Establish()
    {
        _compilation = Analyzed.Compile(RecoveringSource.Files());
        _generator = new(new ApplicationModelAnalyzer(), _emitter);
    }

    void Because() => _result = _generator.Generate(_compilation, new ScreenplayOptions());

    ScreenplayDiagnostic Reported => _result.Diagnostics.First(_ => _.Code == ScreenplayDiagnosticCodes.SourceDidNotCompile);

    [Fact] void should_be_generating_from_source_that_really_does_not_compile() => Analyzed.ErrorsIn(RecoveringSource.Files()).ShouldNotBeEmpty();
    [Fact] void should_be_printing_a_document_the_language_really_rejects() => new ScreenplayCompiler().Compile(Rejected).Success.ShouldBeFalse();
    [Fact] void should_report_the_source_it_read_only_as_a_warning() => Reported.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_report_the_document_on_top_of_it() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.DocumentDidNotCompile);
    [Fact] void should_still_return_the_document() => _result.Source.ShouldEqual(Rejected);
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
}
