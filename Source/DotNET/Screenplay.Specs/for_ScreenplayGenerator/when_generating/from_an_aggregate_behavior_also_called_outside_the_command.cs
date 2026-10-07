// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_aggregate_behavior_also_called_outside_the_command : a_generated_document
{
    void Because()
    {
        var source = AggregateRoutingSources.With("public Task Handle(Author author) => author.Register(Name);") + """
            public class Notifications : Cratis.Chronicle.Reactors.IReactor
            {
                public Task Handle(AuthorRegistered @event, Cratis.Chronicle.Events.EventContext context, Author author) => author.Register(@event.Name);
            }
            """;
        Analyzed.ErrorsIn(Analyzed.Compile((Analyzed.SlicePath, source))).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));
    }

    [Fact] void should_not_inline_the_event_shared_with_the_reactor() => Result.Source.ShouldNotContain("produces event AuthorRegistered");
    [Fact] void should_keep_the_command_production() => Result.Source.ShouldContain("produces AuthorRegistered");
    [Fact] void should_count_the_external_behavior_invocation() => Result.Model.EventProducerCounts.Values.Single().ShouldBeGreaterThan(1);
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
