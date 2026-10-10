// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_event_routes;

public class a_command_route : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Command, EventSourceType("Account", concurrency: true), EventStreamType("Transactions", concurrency: true)]
        public record RegisterAuthor(AuthorId AuthorId, string Month, string Name) : ICanProvideEventStreamId
        {
            public EventStreamId GetEventStreamId() => Month;
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_declare_the_source() => Result.Source.ShouldContain("eventsource Account");
    [Fact] void should_declare_the_stream_key() => Result.Source.ShouldContain("streamId String");
    [Fact] void should_route_the_command() => Result.Source.ShouldContain("stream Account.Transactions");
    [Fact] void should_map_the_command_input() => Result.Source.ShouldContain("streamId = month");
    [Fact] void should_replace_legacy_concurrency() => Result.Source.ShouldNotContain("concurrency");
    [Fact] void should_report_the_dropped_concurrency_dimensions() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EventSourceNotRepresentable).Message.ShouldContain("concurrency dimensions [source type 'Account', stream type 'Transactions']");
    [Fact] void should_classify_the_loss_as_information() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EventSourceNotRepresentable).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_name_the_executable_concurrency_limit() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EventSourceNotRepresentable).Message.ShouldContain("PLAY0271");
    [Fact] void should_select_v8() => Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V8);
    [Fact] void should_bind_without_warnings() => Result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning).ShouldBeEmpty();
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
