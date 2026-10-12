// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_a_closing_event_in_a_returned_batch : a_returned_event_batch
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void should_withhold_a_batch_only_if_the_stream_closes_before_its_last_event(bool closesLast, bool authoring)
    {
        var events = closesLast
            ? "new AuthorWelcomed(@event.Name, \"Welcome\"), new AuthorFiled(@event.Country)"
            : "new AuthorFiled(@event.Country), new AuthorWelcomed(@event.Name, \"Welcome\")";
        GenerateBatch(events, Constraint("{ builder.Unique<AuthorRegistered>(); builder.ClosesStreamOn<AuthorFiled>(); }"), authoring);

        if (closesLast)
        {
            Result.Source.ShouldContain("produces AuthorFiled");
            Result.Diagnostics.Where(_ => _.Code == "SP0070").ShouldBeEmpty();
        }
        else
        {
            AssertWithheld();
            Result.Diagnostics.Single(_ => _.Code == "SP0070").Message.ShouldContain("closes the stream");
        }

        AssertDocument();
    }

    [Fact]
    public void should_read_a_closing_attribute_on_the_event()
    {
        Generate([
            ("Library/Authors/Registration/Registration.cs", Registration),
            ("Library/Authors/Welcoming/Welcoming.cs", Welcoming.Replace("public record AuthorFiled", "[Cratis.Chronicle.Events.Constraints.ClosesStream]\npublic record AuthorFiled", StringComparison.Ordinal) + """
                public class Welcomer : IReactor
                {
                    public IEnumerable<object> Welcome(AuthorRegistered @event) => [new AuthorFiled(@event.Country), new AuthorWelcomed(@event.Name, "Welcome")];
                }
                """)
        ]);

        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Diagnostics.Single(_ => _.Code == "SP0070").Message.ShouldContain("closes the stream");
        AssertDocument();
    }
}
