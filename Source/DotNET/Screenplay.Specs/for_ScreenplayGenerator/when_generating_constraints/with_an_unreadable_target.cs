// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_constraints;

public class with_an_unreadable_target : a_reacting_application
{
    [Theory]
    [InlineData(".On(new Cratis.Chronicle.Events.EventType(\"sent\", 1), [\"Name\"])")]
    [InlineData(".On<WelcomeSent>(e => e.Name.ToLower())")]
    [InlineData(".On<WelcomeSent>(e => e.Name, e => e.Name.ToLower())")]
    [InlineData(".RemovedWith(new Cratis.Chronicle.Events.EventType(\"filed\", 1))")]
    [InlineData(".RemovedWith<AuthorFiled>(e => e.Country)")]
    public void should_withhold_the_whole_constraint(string target)
    {
        GenerateWith($$"""
            public class AuthorConstraint : Cratis.Chronicle.Events.Constraints.IConstraint
            {
                public void Define(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder) =>
                    builder.Unique(unique => unique.WithName("UniqueAuthors").On<AuthorWelcomed>(e => e.Name){{target}});
            }
            """);

        Result.Source.ShouldNotContain("constraint ");
        Result.Source.ShouldNotContain("unique name on AuthorWelcomed");
        Result.Diagnostics.Single(_ => _.Code == "SP0017").Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
        AssertDocument();
    }
}
