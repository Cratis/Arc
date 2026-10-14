// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class from_a_reactor_handling_refusals_fluently : a_refusing_application
{
    [Theory]
    [InlineData("Refused.ByValidation", "on refused by validation")]
    [InlineData("Refused.ByAuthorization", "on refused by authorization")]
    [InlineData("Refused.ByAnyConstraint", "on refused by constraint")]
    [InlineData("Refused.ByConstraint(\"UniqueAuthor\")", "on refused by constraint UniqueAuthor")]
    [InlineData("Refused.Any", "on refused\n")]
    public void should_extract_each_selector_in_both_modes_and_handler_shapes(string selector, string expected)
    {
        foreach (var authoring in new[] { false, true })
        {
            foreach (var asynchronous in new[] { false, true })
            {
                GenerateRefusal($".OnRefused({selector}, refusal => new AuthorFiled(refusal.Message))", authoring, asynchronous);
                Result.Source.ShouldContain(expected);
                Result.Source.ShouldContain("country = $refusal.message");
                Result.Source.ShouldContain("event AuthorFiled");
                AssertRefusalExtraction();
            }
        }
    }

    [Theory]
    [InlineData(".Acknowledge(Refused.ByValidation)")]
    [InlineData(".OnRefused(Refused.ByValidation, refusal => null)")]
    [InlineData(".OnRefused(Refused.ByValidation, refusal => { return null; })")]
    public void should_extract_acknowledgment(string chain)
    {
        GenerateRefusal(chain);
        Result.Source.ShouldContain("on refused by validation");
        Result.Source.ShouldContain("acknowledge");
        Result.Model.Slices.SelectMany(slice => slice.Reactors).Single().Reactions.Single().Invokes.Single().OnRefused.Single().Produces.ShouldBeEmpty();
        AssertRefusalExtraction();
    }

    [Fact]
    public void should_not_recognize_a_lookalike_branch_method()
    {
        GenerateWith("""
            public class Welcomer : IReactor
            {
                public object Welcome(AuthorRegistered @event) => new Lookalike().OnRefused(Refused.Any, _ => null);
            }
            public class Lookalike
            {
                public object OnRefused(Refused selector, Func<Cratis.Arc.Commands.CommandRefusal, object?> handler) => this;
            }
            """);

        Result.Source.ShouldContain("file Authors/Welcoming/Welcoming.cs");
        Result.Diagnostics.Where(report => report.Code == "SP0071").ShouldBeEmpty();
        AssertDocument();
    }
}
