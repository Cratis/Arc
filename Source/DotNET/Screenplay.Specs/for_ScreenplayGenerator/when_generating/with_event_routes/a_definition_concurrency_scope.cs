// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_event_routes;

public class a_definition_concurrency_scope : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Cratis.Chronicle.EventSources.EventSource(Concurrency = Cratis.Chronicle.EventSources.ConcurrencyDimensions.EventSourceId)]
        [Cratis.Chronicle.EventSources.EventStream("Registration")]
        public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;
        [Command, Cratis.Arc.Chronicle.Commands.EventSource<AuthorEventSource>("Registration")]
        public record RegisterAuthor(AuthorId AuthorId, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_state_the_route() => Result.Source.ShouldContain("stream Author.Registration");
    [Fact] void should_not_emit_legacy_concurrency() => Result.Source.ShouldNotContain("concurrency");
    [Fact] void should_report_the_dropped_source_id_dimension() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EventSourceNotRepresentable).Message.ShouldContain("concurrency dimensions [event source id]");
    [Fact] void should_report_the_loss_as_information() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EventSourceNotRepresentable).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_name_the_concurrency_limit() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EventSourceNotRepresentable).Message.ShouldContain("PLAY0271");
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
