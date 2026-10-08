// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

/// <summary>
/// Chronicle narrows which occurrences reach a reactor by tag, event source type, stream type, event source definition
/// and observed sequence. A reaction set off by an event says it runs for every occurrence in the event log, so a
/// narrowed reactor keeps its file reference; one naming the event log explicitly is not narrowed.
/// </summary>
public class from_a_reactor_narrowed_by_chronicle : a_reacting_application
{
    const string Handler = "public AuthorWelcomed Welcome(AuthorRegistered @event) => new(@event.Name, \"Welcome\");";

    [Theory]
    [InlineData("[Cratis.Chronicle.FilterEventsByTag(\"vip\")]", "")]
    [InlineData("[EventSourceType(\"author\")]", "")]
    [InlineData("[EventStreamType(\"onboarding\")]", "")]
    [InlineData("[EventStore(\"elsewhere\")]", "")]
    [InlineData("[Cratis.Chronicle.EventSequences.EventSequence(\"outbox\")]", "")]
    [InlineData("[Reactor(eventSequence: \"outbox\")]", "")]
    [InlineData("[Cratis.Chronicle.EventSources.FromEventSource<AuthorEventSource>(\"Onboarding\")]", "[Cratis.Chronicle.EventSources.EventSource] [Cratis.Chronicle.EventSources.EventStream(\"Onboarding\")] public class AuthorEventSource : Cratis.Chronicle.EventSources.IEventSource;")]
    [InlineData("", "[EventType] [EventStore(\"elsewhere\")] public record AuthorImported(string Name);")]
    public void should_keep_pointing_at_the_file(string attribute, string declarations)
    {
        var imported = declarations.Contains("AuthorImported", StringComparison.Ordinal) ? "public void Import(AuthorImported @event) { }" : string.Empty;
        GenerateWith($$"""{{declarations}} {{attribute}} public class Welcomer : IReactor { {{Handler}} {{imported}} }""");

        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("produces AuthorWelcomed");
        AssertDocument();
    }

    [Theory]
    [InlineData("[Cratis.Chronicle.EventSequences.EventLog]")]
    [InlineData("[Cratis.Chronicle.EventSequences.EventSequence(\"event-log\")]")]
    [InlineData("[Reactor(eventSequence: \"event-log\")]")]
    public void should_state_a_reactor_naming_the_event_log(string attribute)
    {
        GenerateWith($$"""{{attribute}} public class Welcomer : IReactor { {{Handler}} }""");

        Result.Source.ShouldContain("produces AuthorWelcomed");
        AssertDocument();
    }
}
