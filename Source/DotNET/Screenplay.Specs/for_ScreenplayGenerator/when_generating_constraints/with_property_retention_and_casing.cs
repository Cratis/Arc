// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_constraints;

public class with_property_retention_and_casing : a_reacting_application
{
    [Theory]
    [InlineData(".WithMode(Cratis.Chronicle.Events.Constraints.UniqueConstraintMode.PerValue)")]
    [InlineData(".WithMode(System.DateTime.UtcNow.Day == 1 ? Cratis.Chronicle.Events.Constraints.UniqueConstraintMode.PerValue : Cratis.Chronicle.Events.Constraints.UniqueConstraintMode.PerEventSource)")]
    public void should_withhold_unrepresentable_or_unproven_retention(string configuration)
    {
        GenerateConstraint(configuration);

        Result.Source.ShouldNotContain("constraint UniqueNames");
        Result.Diagnostics.Single(_ => _.Code == "SP0017").Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
        AssertDocument();
    }

    [Fact]
    public void should_preserve_case_insensitive_default_retention()
    {
        GenerateConstraint(".WithMode(Cratis.Chronicle.Events.Constraints.UniqueConstraintMode.PerEventSource).IgnoreCasing()");

        Result.Source.ShouldContain("constraint UniqueNames");
        Result.Source.ShouldContain("ignore casing");
        Result.Diagnostics.Where(_ => _.Code == "SP0017").ShouldBeEmpty();
        AssertDocument();
    }

    void GenerateConstraint(string configuration) => GenerateWith($$"""
        public class AuthorConstraint : Cratis.Chronicle.Events.Constraints.IConstraint
        {
            public void Define(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder) =>
                builder.Unique(unique => unique.WithName("UniqueNames").On<AuthorWelcomed>(e => e.Name){{configuration}});
        }
        """);
}
