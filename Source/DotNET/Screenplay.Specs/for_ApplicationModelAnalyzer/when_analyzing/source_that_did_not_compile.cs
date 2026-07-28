// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Source that does not build still yields symbols, and analyzing them produces a document that looks like an
/// answer while describing an application that does not exist. A nearly empty document handed back with nothing
/// wrong reported is the one outcome nobody can act on, so this is an error - which is what makes a host exit non
/// zero - while whatever was recovered is still returned for whoever wants to look at it.
/// </summary>
public class source_that_did_not_compile : Specification
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

    [Fact] void should_be_analyzing_source_that_really_does_not_compile() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldNotBeEmpty();
    [Fact] void should_report_that_the_source_did_not_compile() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.SourceDidNotCompile);
    [Fact] void should_report_it_as_an_error() => Reported.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Error);
    [Fact] void should_say_the_document_cannot_be_relied_on() => Reported.Message.Contains("describes the application reliably", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_quote_the_first_thing_the_compiler_said() => Reported.Message.Contains("ThisDoesNotExist", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_still_return_whatever_it_recovered() => _analysis.Slice().Commands.Single().Name.ShouldEqual("RegisterAuthor");
    [Fact] void should_not_throw_instead() => _analysis.Model.ShouldNotBeNull();
}
