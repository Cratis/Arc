// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_append_metadata_and_returned_events : a_filtered_application
{
    void Because() => GenerateFilteredWith("""
        [EventSourceType("Unknown")]
        public class Welcomer : IReactor
        {
            public AuthorWelcomed Welcome(AuthorRegistered @event) => new(@event.Name, "Welcome");
        }
        [EventSourceType("Unknown")]
        public class ArrayWelcomer : IReactor
        {
            public AuthorWelcomed[] Welcome(AuthorRegistered @event) => [new(@event.Name, "Welcome")];
        }
        [EventSourceType("Unknown")]
        public class EnumerableWelcomer : IReactor
        {
            public IEnumerable<AuthorWelcomed> Welcome(AuthorRegistered @event) => [new(@event.Name, "Welcome")];
        }
        [EventSourceType("Unknown")]
        public class TaskWelcomer : IReactor
        {
            public Task<AuthorWelcomed> Welcome(AuthorRegistered @event) => Task.FromResult(new AuthorWelcomed(@event.Name, "Welcome"));
        }
        [EventSourceType("Unknown")]
        public class TupleWelcomer : IReactor
        {
            public ValueTask<(AuthorWelcomed, SendWelcome)> Welcome(AuthorRegistered @event) => new((new(@event.Name, "Welcome"), new(@event.Name, @event.Country)));
        }
        [EventSourceType("Unknown")]
        public class ResultWelcomer : IReactor
        {
            public Task<Cratis.Monads.Result<AuthorWelcomed, SendWelcome>> Welcome(AuthorRegistered @event) => Task.FromResult<Cratis.Monads.Result<AuthorWelcomed, SendWelcome>>(new AuthorWelcomed(@event.Name, "Welcome"));
        }
        """);

    [Fact] void should_report_append_routing_for_each_event_carrying_signature() => Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableReactionAppendRoute).ShouldEqual(6);
    [Fact] void should_keep_routed_productions_in_source() => Result.Source.ShouldNotContain("produces AuthorWelcomed");
    [Fact] void should_not_execute_the_mixed_commands() => Result.Source.ShouldNotContain("invokes SendWelcome");
    [Fact] void should_compile_and_round_trip() => AssertDocument();
}
