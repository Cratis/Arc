// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_raw_stored_filter_names : a_generated_document
{
    [Theory]
    [InlineData("Author", "\"All\"", false)]
    [InlineData("stored-author", "\"Onboarding\"", false)]
    [InlineData("stored-author", "global::Filters.EventSources.AuthorEventSource.Unused", false)]
    [InlineData("stored-author", "\"stored-onboarding\"", true)]
    [InlineData("stored-author", "global::Filters.EventSources.AuthorEventSource.Onboarding", true)]
    void should_match_only_stored_filter_names(string source, string stream, bool admitted)
    {
        const string Definition = """
                namespace Filters.EventSources;
                [global::Cratis.Chronicle.EventSources.EventSource("stored-author")]
                [global::Cratis.Chronicle.EventSources.EventStream(AuthorEventSource.Onboarding)]
                public class AuthorEventSource : global::Cratis.Chronicle.EventSources.IEventSource
                {
                    public const string Onboarding = "stored-onboarding";
                    public const string Unused = "Onboarding";
                }
                """;
        var reactor = $$"""
                namespace Filters.Work.Processing.FollowUp;
                [global::Cratis.Chronicle.Events.EventType]
                public record Started(string Id);
                [global::Cratis.Chronicle.Events.EventType]
                public record Followed(string Id);
                [global::Cratis.Chronicle.Reactors.Reactor("Worker")]
                [global::Cratis.Chronicle.Events.EventSourceType("{{source}}")]
                [global::Cratis.Chronicle.Events.EventStreamType({{stream}})]
                public class Worker : global::Cratis.Chronicle.Reactors.IReactor
                {
                    public global::Cratis.Chronicle.EventSequences.EventForEventSourceId Started(Started @event) => new global::Cratis.Chronicle.EventSequences.EventForEventSourceId(@event.Id, new Followed(@event.Id));
                }
                """;
        Generate(("Filters/EventSources/AuthorEventSource.cs", Definition), ("Filters/Work/Processing/FollowUp/Worker.cs", reactor));

        Result.Source.Contains("from Author.Onboarding", StringComparison.Ordinal).ShouldEqual(admitted);
        Result.Source.Contains("produces Followed", StringComparison.Ordinal).ShouldEqual(admitted);
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ObserverEventSourceNotRepresentable).ShouldEqual(!admitted);
        if (source == "Author")
        {
            Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.ObserverEventStreamNotDeclared).ShouldBeEmpty();
        }
        AssertDocument();
    }
}
