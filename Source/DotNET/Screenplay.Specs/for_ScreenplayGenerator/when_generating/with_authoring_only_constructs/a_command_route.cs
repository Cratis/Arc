// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class a_command_route : an_authoring_document
{
    void Because() => Generate(IdentifierSources.With("""
        [Command]
        [EventSourceType("Account", concurrency: true)]
        [EventStreamType("Transactions", concurrency: true)]
        public record RegisterAuthor(AuthorId AuthorId, string Month, string Name) : ICanProvideEventStreamId
        {
            public EventStreamId GetEventStreamId() => Month;
            public AuthorRegistered Handle() => new(Name);
        }
        """));

    [Fact] void should_declare_the_source_and_stream() => Result.Source.ShouldContain("eventsource Account");
    [Fact] void should_declare_the_identity_type() => Result.Source.ShouldContain("identifier AuthorId");
    [Fact] void should_declare_the_stream_id_type() => Result.Source.ShouldContain("streamId String");
    [Fact] void should_route_the_command() => Result.Source.ShouldContain("stream Account.Transactions");
    [Fact] void should_map_the_stream_id() => Result.Source.ShouldContain("streamId = month");
    [Fact] void should_keep_the_grammar_supported_source_flag() => Result.Source.ShouldContain("sourceType Account");
    [Fact] void should_keep_the_grammar_supported_stream_flag() => Result.Source.ShouldContain("streamType Transactions");
    [Fact] void should_compile_and_reject_only_executable_admission() => AssertAuthoringDocument();
    [Fact] void should_omit_routing_when_disabled() => Off.Source.Contains("eventsource Account", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_add_build_warnings_in_default_mode() => Off.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning).ShouldBeEmpty();
    [Fact] void should_report_omitted_routing_as_information() => Off.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EventSourceNotRepresentable).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_report_the_opt_in_when_disabled() => Off.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EventSourceNotRepresentable).Message.ShouldContain("AuthoringOnlyConstructs");
}
