// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_constrained_events_for_event_source_ids : a_returned_event_batch
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_state_one_atomic_batch_across_explicit_destinations(bool authoring)
    {
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring },
        [
            ("Library/Authors/Registration/Registration.cs", Registration),
            ("Library/Authors/Welcoming/Welcoming.cs", Welcoming + """
                public class Welcomer : IReactor
                {
                    public IEnumerable<Cratis.Chronicle.EventSequences.EventForEventSourceId> Welcome(AuthorRegistered @event) =>
                    [
                        new(@event.Name, new AuthorWelcomed(@event.Name, "Welcome")),
                        new(@event.Country, new AuthorFiled(@event.Country))
                    ];
                }
                """ + Constraint("=> builder.Unique<AuthorFiled>();"))
        ]);

        AssertAtomicBatch(scenario: false);
        Result.Source.ShouldContain("for name");
        Result.Source.ShouldContain("for country");
    }
}
