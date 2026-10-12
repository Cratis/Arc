// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_an_intra_batch_conflict : a_returned_event_batch
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_withhold_events_that_can_conflict_together(bool authoring)
    {
        GenerateBatch("new AuthorFiled(@event.Country), new AuthorFiled(@event.Country)", Constraint("=> builder.Unique<AuthorFiled>();"), authoring);

        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Source.ShouldNotContain("produces AuthorFiled");
        Result.Diagnostics.Single(_ => _.Code == "SP0070").Message.ShouldContain("FilingConstraint");
        AssertDocument();
    }
}
