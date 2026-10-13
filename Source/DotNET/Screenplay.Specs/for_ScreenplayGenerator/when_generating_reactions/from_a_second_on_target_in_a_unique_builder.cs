// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_a_second_on_target_in_a_unique_builder : a_returned_event_batch
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_read_every_target_not_only_the_first(bool authoring)
    {
        GenerateBatch("new AuthorWelcomed(@event.Name, \"Welcome\"), new AuthorFiled(@event.Country)", Constraint("=> builder.Unique(unique => unique.On<AuthorRegistered>(e => e.Country).On<AuthorFiled>(e => e.Country));"), authoring);

        AssertWithheld();
        Result.Source.ShouldContain("unique country on AuthorRegistered");
        Result.Source.ShouldContain("unique country on AuthorFiled");
        Result.Diagnostics.Single(_ => _.Code == "SP0070").Message.ShouldContain("AuthorFiled");
    }
}
