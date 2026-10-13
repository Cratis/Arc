// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_event_routes;

public class with_const_backed_names : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Cratis.Chronicle.EventSources.EventSource("stored-account")]
        [Cratis.Chronicle.EventSources.EventStream(AccountEventSource.Transactions)]
        public class AccountEventSource : Cratis.Chronicle.EventSources.IEventSource
        {
            public const string Transactions = "stored-transactions";
        }
        [Command, Cratis.Arc.Chronicle.Commands.EventSource<AccountEventSource>(AccountEventSource.Transactions)]
        public record RegisterAuthor(AuthorId AuthorId, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_recover_the_source_model_name() => Result.Source.ShouldContain("eventsource Account");
    [Fact] void should_preserve_the_stored_source_name() => Result.Source.ShouldContain("id \"stored-account\"");
    [Fact] void should_preserve_the_stored_stream_name() => Result.Source.ShouldContain("id \"stored-transactions\"");
    [Fact] void should_route_the_command_with_model_names() => Result.Source.ShouldContain("stream Account.Transactions");
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
