// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing.source_that_did_not_compile;

/// <summary>
/// Something was recovered, but every declaration it came out of is one the compiler could not make sense of - so
/// what the document says about each of them is read from source that never held together. That is the other way
/// recovery is genuinely prevented, and it is an error for the same reason as recovering nothing at all: there is
/// no part of the document a reader could trust.
/// </summary>
public class and_every_artifact_it_recovered_carries_an_error : Specification
{
    const string Source = """
        using Cratis.Arc.Commands.ModelBound;

        namespace Library.Authors.Registration;

        [Command]
        public record RegisterAuthor(string Name)
        {
            public void Handle() => ThisDoesNotExist();
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(Source);

    ScreenplayDiagnostic Reported => _analysis.Diagnostics.First(_ => _.Code == ScreenplayDiagnosticCodes.SourceDidNotCompile);

    [Fact] void should_be_analyzing_source_that_really_does_not_compile() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldNotBeEmpty();
    [Fact] void should_report_it_as_an_error() => Reported.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Error);
    [Fact] void should_say_how_much_it_recovered() => Reported.Message.Contains("1 artifact(s) were recovered", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_say_the_errors_sit_inside_what_it_recovered() => Reported.Message.Contains("every declaration they were read from is one an error sits inside", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_say_the_document_cannot_be_relied_on() => Reported.Message.Contains("describes the application reliably", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_still_return_whatever_it_recovered() => _analysis.Slice().Commands.Single().Name.ShouldEqual("RegisterAuthor");
}
