// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_event_routes;

public class with_a_conflicting_observer_declaration : a_generated_document
{
    const string Definition = """
            namespace Library.EventSources;
            [Cratis.Chronicle.EventSources.EventSource("stored-account")]
            [Cratis.Chronicle.EventSources.EventStream(AccountEventSource.Transactions)]
            public class AccountEventSource : Cratis.Chronicle.EventSources.IEventSource
            {
                public const string Transactions = "stored-transactions";
            }
            """;
    const string Behavior = """
            [Command, EventSourceType("Account"), EventStreamType("Transactions")]
            public record RegisterAuthor(AuthorId AuthorId, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            [Cratis.Chronicle.Reactors.Reactor("Worker")]
            [Cratis.Chronicle.EventSources.FromEventSource<Library.EventSources.AccountEventSource>(Library.EventSources.AccountEventSource.Transactions)]
            public class Worker : Cratis.Chronicle.Reactors.IReactor
            {
                public Cratis.Chronicle.EventSequences.EventForEventSourceId Started(Started @event) => new Cratis.Chronicle.EventSequences.EventForEventSourceId(@event.Name, new Followed(@event.Name));
            }
            [EventType]
            public record Started(string Name);
            [EventType]
            public record Followed(string Name);
            """;

    void Because() => Generate(("Library/EventSources/Account.cs", Definition), (Analyzed.SlicePath, IdentifierSources.With(Behavior)));

    [Fact] void should_withhold_the_conflicting_command_route() => Result.Source.ShouldNotContain("stream Account.Transactions");
    [Fact] void should_preserve_the_observer_filter() => Result.Source.ShouldContain("from Account.Transactions");
    [Fact] void should_preserve_the_observed_stored_source() => Result.Source.ShouldContain("id \"stored-account\"");
    [Fact] void should_report_the_withheld_command_route() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableCommandRoute).ShouldContainSingleItem();
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
