// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_constraints;

public class with_unique_event_releases : a_reacting_application
{
    [Fact]
    public void should_keep_every_release_with_its_event_constraint()
    {
        GenerateWith("""
            public class AuthorConstraint : Cratis.Chronicle.Events.Constraints.IConstraint
            {
                public void Define(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder)
                {
                    builder.Unique<AuthorWelcomed>(name: "OneWelcome").RemovedWith<AuthorFiled>();
                    builder.RemovedWith<AuthorGreeted>();
                    builder.Unique<WelcomeSent>(name: "OneSend").RemovedWith<AuthorRegistered>();
                }
            }
            """);

        Result.Source.ShouldContain("constraint OneWelcome\n        unique event AuthorWelcomed\n        released by AuthorFiled\n        released by AuthorGreeted");
        Result.Source.ShouldContain("constraint OneSend\n        unique event WelcomeSent\n        released by AuthorRegistered");
        Result.Diagnostics.Where(_ => _.Code == "SP0017").ShouldBeEmpty();
        AssertDocument();
    }
}
