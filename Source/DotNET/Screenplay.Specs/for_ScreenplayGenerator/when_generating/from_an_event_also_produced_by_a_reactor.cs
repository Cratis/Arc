// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_event_also_produced_by_a_reactor : a_batch_a_document
{
    const string ReactorSource = """
        using Library.Authors.Registration;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;
        namespace Library.Authors.Notifications;
        public class Notifications : IReactor
        {
            public AuthorRegistered Handle(AuthorRegistered @event, EventContext context) => new(@event.Name);
        }
        """;

    void Because() => Generate(
        (Analyzed.SlicePath, IdentifierSources.With("""
            [Command]
            public record RegisterAuthor(AuthorId Id, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            """)),
        ("Library/Authors/Notifications/Notify.cs", ReactorSource));

    [Fact] void should_keep_the_event_standalone() => Result.Source.ShouldNotContain("produces event AuthorRegistered");
    [Fact] void should_count_the_reactor_construction() => Result.Model.EventProducerCounts.Values.Single().ShouldEqual(2);
    [Fact] void should_read_back_without_errors() => RoundTrip.Errors.ShouldBeEmpty();
    [Fact] void should_round_trip_stably() => RoundTrip.IsStable.ShouldBeTrue();
}
