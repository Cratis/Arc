// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_a_model_stream_named_all : a_generated_document
{
    const string Definition = """
            namespace Filters.EventSources;
            [global::Cratis.Chronicle.EventSources.EventSource("stored-account")]
            [global::Cratis.Chronicle.EventSources.EventStream(AccountEventSource.All)]
            public class AccountEventSource : global::Cratis.Chronicle.EventSources.IEventSource
            {
                public const string All = "stored-all";
            }
            """;
    const string Reactor = """
            namespace Filters.Work.Processing.FollowUp;
            [global::Cratis.Chronicle.Events.EventType]
            public record Started(string Id);
            [global::Cratis.Chronicle.Events.EventType]
            public record Followed(string Id);
            [global::Cratis.Chronicle.Reactors.Reactor("Worker")]
            [global::Cratis.Chronicle.EventSources.FromEventSource<global::Filters.EventSources.AccountEventSource>(global::Filters.EventSources.AccountEventSource.All)]
            public class Worker : global::Cratis.Chronicle.Reactors.IReactor
            {
                public global::Cratis.Chronicle.EventSequences.EventForEventSourceId Started(Started @event) => new global::Cratis.Chronicle.EventSequences.EventForEventSourceId(@event.Id, new Followed(@event.Id));
            }
            """;

    void Because() => Generate(("Filters/EventSources/AccountEventSource.cs", Definition), ("Filters/Work/Processing/FollowUp/Worker.cs", Reactor));

    [Fact] void should_keep_the_filter_stream_scoped() => Result.Source.ShouldContain("from Account.All");
    [Fact] void should_preserve_the_stored_stream() => Result.Source.ShouldContain("id \"stored-all\"");
    [Fact] void should_recover_the_effect() => Result.Source.ShouldContain("produces Followed");
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
