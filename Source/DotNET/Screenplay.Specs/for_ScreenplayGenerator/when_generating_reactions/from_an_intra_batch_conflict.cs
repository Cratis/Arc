// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_an_intra_batch_conflict : a_returned_event_batch
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_state_events_that_can_conflict_together_atomically(bool authoring)
    {
        GenerateBatch("new AuthorFiled(@event.Country), new AuthorFiled(@event.Country)", Constraint("=> builder.Unique<AuthorFiled>();"), authoring);

        Result.Source.ShouldContain("produces AuthorFiled");
        AssertAtomicBatch(scenario: false);
        Result.Source.ShouldNotContain($"specification {ScenarioName}");
    }
}
