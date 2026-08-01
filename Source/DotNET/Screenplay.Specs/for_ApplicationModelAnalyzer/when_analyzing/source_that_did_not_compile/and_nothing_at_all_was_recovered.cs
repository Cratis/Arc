// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing.source_that_did_not_compile;

/// <summary>
/// A build that produced nothing an artifact could be read out of is the case the claim was always true for - a
/// document describing an empty application handed back with nothing wrong reported is the one outcome nobody can
/// act on, so this is an error and a host exits non zero on it. It is also the case that must not be mistaken for
/// an application which simply declares no artifact yet, so "declares nothing" stays suppressed.
/// </summary>
public class and_nothing_at_all_was_recovered : Specification
{
    const string Source = """
        namespace Library.Plumbing;

        public class NotAnArtifact
        {
            public string Name() => ThisDoesNotExist;
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    ScreenplayDiagnostic Reported => _analysis.Diagnostics.First(_ => _.Code == ScreenplayDiagnosticCodes.SourceDidNotCompile);

    [Fact] void should_be_analyzing_source_that_really_does_not_compile() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldNotBeEmpty();
    [Fact] void should_report_that_the_source_did_not_compile() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.SourceDidNotCompile);
    [Fact] void should_report_it_as_an_error() => Reported.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Error);
    [Fact] void should_say_nothing_at_all_came_out_of_it() => Reported.Message.Contains("Nothing at all was recovered from it", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_say_the_document_cannot_be_relied_on() => Reported.Message.Contains("describes the application reliably", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_quote_the_first_thing_the_compiler_said() => Reported.Message.Contains("ThisDoesNotExist", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_also_say_the_application_declares_nothing() => _analysis.Diagnostics.Select(_ => _.Code).ShouldNotContain(ScreenplayDiagnosticCodes.AnalysisUnavailable);
    [Fact] void should_recover_no_slice() => _analysis.Model.Slices.ShouldBeEmpty();
    [Fact] void should_not_throw_instead() => _analysis.Model.ShouldNotBeNull();
}
