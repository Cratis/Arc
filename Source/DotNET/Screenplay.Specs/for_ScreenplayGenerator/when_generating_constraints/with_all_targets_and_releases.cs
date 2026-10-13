// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_constraints;

public class with_all_targets_and_releases : a_reacting_application
{
    [Theory]
    [InlineData("unique => unique.WithName(\"UniqueAuthors\").On<AuthorWelcomed>(e => e.Name)", "unique name on AuthorWelcomed", "constraint UniqueAuthors")]
    [InlineData("unique => { unique.WithName(\"UniqueAuthors\").On<AuthorWelcomed>(e => e.Name); unique.On<WelcomeSent>(e => e.Name); }", "unique name on AuthorWelcomed", "unique name on WelcomeSent")]
    [InlineData("unique => unique.WithName(\"UniqueAuthors\").On<AuthorWelcomed>(e => e.Name).On<WelcomeSent>(e => e.Name)", "unique name on AuthorWelcomed", "unique name on WelcomeSent")]
    [InlineData("unique => unique.WithName(\"UniqueAuthors\").On<AuthorWelcomed>(e => e.Name, e => e.Greeting).RemovedWith<AuthorFiled>().RemovedWith<AuthorGreeted>()", "unique name, greeting on AuthorWelcomed", "released by AuthorFiled")]
    [InlineData("unique => unique.WithName(\"UniqueAuthors\").On<AuthorWelcomed>(e => e.Name).RemovedWith<AuthorFiled>().RemovedWith<AuthorGreeted>()", "released by AuthorFiled", "released by AuthorGreeted")]
    public void should_state_the_whole_claim(string definition, string first, string second)
    {
        GenerateWith($$"""
            public class AuthorConstraint : Cratis.Chronicle.Events.Constraints.IConstraint
            {
                public void Define(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder) => builder.Unique({{definition}});
            }
            """);

        Result.Source.ShouldContain("constraint UniqueAuthors");
        Result.Source.ShouldContain(first);
        Result.Source.ShouldContain(second);
        Result.Diagnostics.Where(_ => _.Code == "SP0017").ShouldBeEmpty();
        AssertDocument();
    }
}
