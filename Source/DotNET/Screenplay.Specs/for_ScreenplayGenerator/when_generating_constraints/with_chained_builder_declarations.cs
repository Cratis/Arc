// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;
using Cratis.Screenplay;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_constraints;

public class with_chained_builder_declarations : a_reacting_application
{
    [Fact]
    public void should_keep_a_generic_release_out_of_the_next_property_claim()
    {
        GenerateDefinition("""
            builder.Unique<AuthorWelcomed>(name: "OneWelcome").RemovedWith<AuthorFiled>()
                .Unique(unique => unique.WithName("UniqueSend").On<WelcomeSent>(e => e.Name));
            """);

        Constraint("OneWelcome").ReleasedBy.ShouldContainOnly(["AuthorFiled"]);
        ((UniquePropertyConstraintSyntax)Constraint("UniqueSend")).Event.ShouldEqual("WelcomeSent");
        Constraint("UniqueSend").ReleasedBy.ShouldBeEmpty();
        Constraint("UniqueSend").AdditionalRules.ShouldBeEmpty();
        Result.Diagnostics.Where(_ => _.Code == "SP0017").ShouldBeEmpty();
        AssertDocument();
    }

    [Theory]
    [InlineData("builder")]
    [InlineData("builder.ForEventLog()")]
    public void should_keep_chained_property_claims_independent(string receiver)
    {
        GenerateDefinition($$"""
            {{receiver}}.Unique(unique => unique.WithName("FirstClaim").On<AuthorWelcomed>(e => e.Name).RemovedWith<AuthorFiled>())
                .Unique(unique => unique.WithName("SecondClaim").On<WelcomeSent>(e => e.Name));
            """);

        ((UniquePropertyConstraintSyntax)Constraint("FirstClaim")).Event.ShouldEqual("AuthorWelcomed");
        Constraint("FirstClaim").ReleasedBy.ShouldContainOnly(["AuthorFiled"]);
        ((UniquePropertyConstraintSyntax)Constraint("SecondClaim")).Event.ShouldEqual("WelcomeSent");
        Constraint("SecondClaim").ReleasedBy.ShouldBeEmpty();
        Constraint("SecondClaim").AdditionalRules.ShouldBeEmpty();
        Result.Diagnostics.Where(_ => _.Code == "SP0017").ShouldBeEmpty();
        AssertDocument();
    }

    void GenerateDefinition(string definition) => GenerateWith($$"""
        public class AuthorConstraint : Cratis.Chronicle.Events.Constraints.IConstraint
        {
            public void Define(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder) => {{definition}}
        }
        """);

    ConstraintSyntax Constraint(string name) => new ScreenplayCompiler().Compile(Result.Source).Value!.Modules
        .SelectMany(_ => _.Features).SelectMany(_ => _.Slices).SelectMany(_ => _.Constraints).Single(_ => _.Name == name);
}
