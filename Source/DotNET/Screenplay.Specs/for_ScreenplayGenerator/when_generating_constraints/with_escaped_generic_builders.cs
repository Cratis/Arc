// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_constraints;

public class with_escaped_generic_builders : a_reacting_application
{
    [Theory]
    [InlineData("var claim = builder.Unique<AuthorWelcomed>(name: \"OneWelcome\"); claim.RemovedWith<AuthorFiled>();")]
    [InlineData("var claim = builder; builder.Unique<AuthorWelcomed>(name: \"OneWelcome\"); claim.RemovedWith<AuthorFiled>();")]
    [InlineData("builder.Unique<AuthorWelcomed>(name: \"OneWelcome\"); Configure(builder);")]
    [InlineData("builder.Unique<AuthorWelcomed>(name: \"OneWelcome\"); System.Action release = () => Configure(builder);")]
    [InlineData("if (System.DateTime.UtcNow.Day == 1) builder.Unique<AuthorWelcomed>(name: \"OneWelcome\");")]
    [InlineData("Other.Unique<AuthorWelcomed>(name: \"OneWelcome\");")]
    public void should_withhold_the_claim_even_without_a_same_symbol_release(string definition)
    {
        GenerateWith($$"""
            public class AuthorConstraint : Cratis.Chronicle.Events.Constraints.IConstraint
            {
                public static Cratis.Chronicle.Events.Constraints.IConstraintBuilder Other { get; set; } = null!;
                public void Define(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder)
                {
                    {{definition}}
                }
                static void Configure(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder) => builder.RemovedWith<AuthorFiled>();
            }
            """);

        Result.Source.ShouldNotContain("constraint OneWelcome");
        Result.Diagnostics.Where(_ => _.Code == "SP0017").ShouldNotBeEmpty();
        AssertDocument();
    }
}
