// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_a_class_level_unique_event_in_a_returned_batch : a_returned_event_batch
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void should_withhold_later_or_repeated_unique_event_types(bool repeated, bool authoring)
    {
        var events = repeated
            ? "new AuthorFiled(@event.Country), new AuthorFiled(@event.Country)"
            : "new AuthorWelcomed(@event.Name, \"Welcome\"), new AuthorFiled(@event.Country)";
        Generate(new ScreenplayOptions { AuthoringOnlyConstructs = authoring },
        [
            ("Library/Authors/Registration/Registration.cs", Registration),
            ("Library/Authors/Welcoming/Welcoming.cs", Welcoming.Replace("public record AuthorFiled", "[Cratis.Chronicle.Events.Constraints.Unique]\npublic record AuthorFiled", StringComparison.Ordinal) + $$"""
                public class Welcomer : IReactor
                {
                    public IEnumerable<object> Welcome(AuthorRegistered @event) => [{{events}}];
                }
                """),
            (IntegrationTesting.Path, IntegrationTesting.Source),
            (ScenarioPath, Scenario)
        ]);

        AssertWithheld();
        Result.Diagnostics.Single(_ => _.Code == "SP0070").Message.ShouldContain("AuthorFiled");
    }
}
