// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_configured_constraints_covering_other_events : a_returned_event_batch
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_unrelated_batches_declarative(bool authoring)
    {
        const string Definition = """
            {
                builder.PerEventSourceType().PerEventStreamType().PerEventStreamId().ForEventLog();
                builder.Unique(unique => unique.On<AuthorRegistered>(e => e.Name)
                    .WithName(nameof(AuthorRegistered)).WithMessage("Only once")
                    .WithMode(Cratis.Chronicle.Events.Constraints.UniqueConstraintMode.PerValue).IgnoreCasing());
                builder.ClosesStreamOn<AuthorGreeted>(closing => closing.PerEventSourceId()
                    .PerEventSourceType().PerEventStreamType().PerEventStreamId()
                    .EventStreamIdFrom(e => e.Name).ReopenedBy<AuthorRegistered>());
            }
            """;
        GenerateBatch("new AuthorWelcomed(@event.Name, \"Welcome\"), new AuthorFiled(@event.Country)", Constraint(Definition), authoring);

        Result.Source.ShouldContain("produces AuthorWelcomed");
        Result.Source.ShouldContain("produces AuthorFiled");
        Result.Source.ShouldContain($"specification {ScenarioName}");
        Result.Diagnostics.Where(_ => _.Code == "SP0070").ShouldBeEmpty();
        AssertDocument();
        Run(ScenarioName).Passed.ShouldBeTrue();
    }
}
