// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_constraints;

public class with_unproven_unique_event_releases : a_reacting_application
{
    [Theory]
    [InlineData("if (System.DateTime.UtcNow.Day == 1) builder.RemovedWith<AuthorFiled>();")]
    [InlineData("System.Action release = () => builder.RemovedWith<AuthorFiled>();")]
    [InlineData("if (System.DateTime.UtcNow.Day == 1) builder.Unique<WelcomeSent>(name: \"OneSend\"); builder.RemovedWith<AuthorFiled>();")]
    [InlineData("Configure(builder); builder.RemovedWith<AuthorFiled>();")]
    public void should_not_state_unproven_releases_as_unconditional(string release)
    {
        GenerateWith($$"""
            public class AuthorConstraint : Cratis.Chronicle.Events.Constraints.IConstraint
            {
                public void Define(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder)
                {
                    builder.Unique<AuthorWelcomed>(name: "OneWelcome");
                    {{release}}
                }
                static void Configure(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder) => builder.Unique<WelcomeSent>();
            }
            """);

        Result.Source.ShouldNotContain("constraint OneWelcome");
        Result.Source.ShouldNotContain("released by AuthorFiled");
        Result.Diagnostics.Where(_ => _.Code == "SP0017").ShouldNotBeEmpty();
        AssertDocument();
    }
}
