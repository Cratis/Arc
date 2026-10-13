// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_constraints;

public class with_unproven_property_builder_statements : a_reacting_application
{
    [Theory]
    [InlineData("unique => { unique.WithName(\"UniqueNames\").On<AuthorWelcomed>(e => e.Name); unique = Enabled ? unique.RemovedWith<AuthorFiled>() : unique; }")]
    [InlineData("unique => { unique.WithName(\"UniqueNames\").On<AuthorWelcomed>(e => e.Name); unique = Enabled ? unique.On<WelcomeSent>(e => e.Name) : unique; }")]
    [InlineData("unique => unique.WithName(\"UniqueNames\").On<AuthorWelcomed>(e => e.Name).WithMessage(_ => { unique.RemovedWith<AuthorFiled>(); return \"Only once\"; })")]
    [InlineData("unique => { unique.WithName(\"UniqueNames\").On<AuthorWelcomed>(e => e.Name); Other.On<WelcomeSent>(e => e.Name).RemovedWith<AuthorFiled>(); }")]
    public void should_withhold_the_whole_claim(string callback)
    {
        GenerateWith($$"""
            public class AuthorConstraint : Cratis.Chronicle.Events.Constraints.IConstraint
            {
                public static Cratis.Chronicle.Events.Constraints.IUniqueConstraintBuilder Other { get; set; } = null!;
                static bool Enabled => System.DateTime.UtcNow.Day == 1;
                public void Define(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder) => builder.Unique({{callback}});
            }
            """);

        Result.Source.ShouldNotContain("constraint UniqueNames");
        Result.Diagnostics.Single(_ => _.Code == "SP0017").Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
        AssertDocument();
    }
}
