// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_a_constrained_event_later_in_a_returned_batch : a_returned_event_batch
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_state_the_atomic_batch_and_its_scenario(bool authoring)
    {
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring },
            [
            ("Library/Authors/Registration/Registration.cs", Registration),
            ("Library/Authors/Welcoming/Welcoming.cs", Welcoming.Replace("string Country);", "[property: Cratis.Chronicle.Events.Constraints.Unique] string Country);", StringComparison.Ordinal) + """
                public class Welcomer : IReactor
                {
                    public IEnumerable<object> Welcome(AuthorRegistered @event) => [new AuthorWelcomed(@event.Name, "Welcome"), new AuthorFiled(@event.Country)];
                }
                """),
            (IntegrationTesting.Path, IntegrationTesting.Source),
            (ScenarioPath, Scenario)
            ]);

        AssertAtomicBatch();
        Run(ScenarioName).Passed.ShouldBeTrue();
    }
}
