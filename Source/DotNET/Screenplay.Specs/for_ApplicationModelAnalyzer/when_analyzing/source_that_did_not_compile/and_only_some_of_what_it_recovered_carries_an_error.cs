// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing.source_that_did_not_compile;

/// <summary>
/// This is the boundary between the two severities. One artifact was read from a declaration an error sits inside
/// and one was not, and the one that was not is described exactly as its source states it - so "nothing recovered
/// describes the application reliably" is already false with a single clean declaration. The severity therefore
/// turns on whether any survived rather than on whether any was hit, and the count says which is which.
/// </summary>
public class and_only_some_of_what_it_recovered_carries_an_error : Specification
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [Command]
        public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle() => new(ThisDoesNotExist);
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    ScreenplayDiagnostic Reported => _analysis.Diagnostics.First(_ => _.Code == ScreenplayDiagnosticCodes.SourceDidNotCompile);

    [Fact] void should_be_analyzing_source_that_really_does_not_compile() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldNotBeEmpty();
    [Fact] void should_report_it_as_a_warning() => Reported.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_report_nothing_at_all_as_an_error() => _analysis.Diagnostics.Any(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeFalse();
    [Fact] void should_count_only_the_one_no_error_sits_inside() => Reported.Message.Contains("2 artifact(s) were recovered anyway, 1 of them", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_quote_the_first_thing_the_compiler_said() => Reported.Message.Contains("ThisDoesNotExist", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_still_return_whatever_it_recovered() => _analysis.Slice().Commands.Single().Name.ShouldEqual("RegisterAuthor");
}
