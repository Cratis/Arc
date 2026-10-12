// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_a_constrained_event_first_in_a_returned_batch : a_returned_event_batch
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_state_the_batch_and_run_its_scenario(bool authoring)
    {
        GenerateBatch("new AuthorWelcomed(@event.Name, \"Welcome\"), new AuthorFiled(@event.Country)", Constraint("=> builder.Unique<AuthorWelcomed>();"), authoring);

        Result.Source.ShouldContain("produces AuthorWelcomed");
        Result.Source.ShouldContain("produces AuthorFiled");
        Result.Source.ShouldContain($"specification {ScenarioName}");
        Result.Diagnostics.Where(_ => _.Code == "SP0070").ShouldBeEmpty();
        ScenarioReports.ShouldBeEmpty();
        AssertDocument();
        Run(ScenarioName).Passed.ShouldBeTrue();
    }
}
