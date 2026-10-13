// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_event_routes;

public class with_literal_identity_names : a_generated_document
{
    const string OriginalDocument = """
        domain Library

        eventsource Account
          identifier AuthorId
          stream Transactions

        concept AuthorId : Uuid

        module Library

          feature Authors

            slice StateChange Registration

              command RegisterAuthor
                authorId AuthorId identifier
                name String
                stream Account.Transactions
                produces event AuthorRegistered
                  for authorId
                  name String = name

        """;

    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        [Cratis.Chronicle.EventSources.EventSource("Account")]
        [Cratis.Chronicle.EventSources.EventStream("Transactions")]
        public class Ledger : Cratis.Chronicle.EventSources.IEventSource;
        [Command, Cratis.Arc.Chronicle.Commands.EventSource<Ledger>("Transactions")]
        public record RegisterAuthor(AuthorId AuthorId, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """)));

    [Fact] void should_keep_the_original_literal_document() => Result.Source.ShouldEqual(OriginalDocument);
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
