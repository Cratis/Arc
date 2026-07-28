// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Screenplay;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Source the compiler never accepted still yields symbols, and a model recovered from them describes an application
/// that does not exist - so a document made from it being poor is the consequence already reported rather than a
/// second defect. Saying both would send whoever reads the diagnostics hunting for a bug in the generator when the
/// thing to fix is the build, which is why the same suppression the empty document already gets applies here.
/// </summary>
public class and_the_source_it_read_did_not_compile : given.a_document_the_language_rejects
{
    const string Source = """
        namespace Library.Authors.Registration;

        public record RegisterAuthor(string Name)
        {
            public string Handle() => ThisDoesNotExist;
        }
        """;

    Compilation _compilation;
    ScreenplayGenerator _generator;
    ScreenplayGenerationResult _result;

    void Establish()
    {
        _compilation = Analyzed.Compile((Analyzed.SlicePath, Source));
        _generator = new(new ApplicationModelAnalyzer(), _emitter);
    }

    void Because() => _result = _generator.Generate(_compilation, new ScreenplayOptions());

    [Fact] void should_be_generating_from_source_that_really_does_not_compile() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldNotBeEmpty();
    [Fact] void should_be_printing_a_document_the_language_really_rejects() => new ScreenplayCompiler().Compile(Rejected).Success.ShouldBeFalse();
    [Fact] void should_report_that_the_source_did_not_compile() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.SourceDidNotCompile);
    [Fact] void should_not_report_the_document_on_top_of_it() => _result.Diagnostics.Select(_ => _.Code).ShouldNotContain(ScreenplayDiagnosticCodes.DocumentDidNotCompile);
    [Fact] void should_still_return_the_document() => _result.Source.ShouldEqual(Rejected);
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
}
