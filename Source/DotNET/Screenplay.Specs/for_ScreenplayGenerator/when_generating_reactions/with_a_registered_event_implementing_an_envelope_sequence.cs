// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_a_registered_event_implementing_an_envelope_sequence : a_filtered_application
{
    const string Reactor = """
        [Cratis.Chronicle.EventSources.EventSource("Author")]
        public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;

        [EventType]
        public record EnvelopeSequence : IEnumerable<Cratis.Chronicle.EventSequences.EventForEventSourceId>
        {
            public IEnumerator<Cratis.Chronicle.EventSequences.EventForEventSourceId> GetEnumerator() =>
                ((IEnumerable<Cratis.Chronicle.EventSequences.EventForEventSourceId>)Array.Empty<Cratis.Chronicle.EventSequences.EventForEventSourceId>()).GetEnumerator();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [EventSourceType("Author")]
        public class Welcomer : IReactor
        {
            public EnvelopeSequence Welcome(AuthorRegistered @event) => new();
        }
        """;

    void Because() => GenerateFilteredWith(Reactor);

    [Fact] void should_keep_the_bare_event_in_source() => Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
    [Fact] void should_not_mistake_the_event_for_explicitly_routed_elements() => Result.Source.ShouldNotContain("produces EnvelopeSequence");
    [Fact] void should_report_the_context_append_route() => Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(ScreenplayDiagnosticCodes.UnrepresentableReactionAppendRoute);
    [Fact] void should_compile_and_round_trip() => AssertDocument();
}
