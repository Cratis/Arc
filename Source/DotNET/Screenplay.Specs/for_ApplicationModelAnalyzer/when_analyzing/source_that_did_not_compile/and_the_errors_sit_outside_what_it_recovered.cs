// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing.source_that_did_not_compile;

/// <summary>
/// This is the case that made the claim a lie. A host handing over a compilation assembled without the compile items
/// a build generates leaves every reference to a strongly typed resource class unresolved, and the errors then sit in
/// source that declares no artifact at all while every command and event is read exactly as written. Calling that an
/// error says something untrue about a document that is entirely correct and makes the host throw it away, so it is a
/// warning that states both facts - what failed, and how much came through anyway.
/// </summary>
public class and_the_errors_sit_outside_what_it_recovered : Specification
{
    const string Slice = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [Command]
        public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    const string Wording = """
        namespace Library.Authors;

        public static class Wording
        {
            public static string NameIsRequired() => AuthorsMessages.NameIsRequired;
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", Slice),
        ("Library/Authors/Wording.cs", Wording)
    ];

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(_sources);

    ScreenplayDiagnostic Reported => _analysis.Diagnostics.First(_ => _.Code == ScreenplayDiagnosticCodes.SourceDidNotCompile);

    [Fact] void should_be_analyzing_source_that_really_does_not_compile() => Analyzed.ErrorsIn(_sources).ShouldNotBeEmpty();
    [Fact] void should_report_that_the_source_did_not_compile() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.SourceDidNotCompile);
    [Fact] void should_report_it_as_a_warning() => Reported.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_report_nothing_at_all_as_an_error() => _analysis.Diagnostics.Any(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeFalse();
    [Fact] void should_say_how_many_errors_there_were() => Reported.Message.Contains("1 error(s)", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_quote_the_first_thing_the_compiler_said() => Reported.Message.Contains("AuthorsMessages", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_say_how_much_it_recovered_anyway() => Reported.Message.Contains("2 artifact(s) were recovered anyway, 2 of them", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_hint_at_what_the_host_left_out() => Reported.Message.Contains("without the compile items a build generates", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_claim_nothing_describes_the_application_reliably() => Reported.Message.Contains("describes the application reliably", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_recover_the_command() => _analysis.Slice().Commands.Single().Name.ShouldEqual("RegisterAuthor");
    [Fact] void should_recover_the_event() => _analysis.Slice().Events.Single().Name.ShouldEqual("AuthorRegistered");
}
