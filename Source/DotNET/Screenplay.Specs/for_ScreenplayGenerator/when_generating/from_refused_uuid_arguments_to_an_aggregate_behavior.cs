// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_refused_uuid_arguments_to_an_aggregate_behavior : a_generated_document
{
    [Theory]
    [InlineData("(Guid)Maybe, Owner", "casts that change the operand type")]
    [InlineData("Owner, (Guid)Maybe", "casts that change the operand type")]
    [InlineData("(Guid)(object)Maybe, Owner", "casts that change the operand type")]
    [InlineData("(Guid)Member, Owner", "casts that change the operand type")]
    [InlineData("Member, (Guid)Plant", "casts that change the operand type")]
    [InlineData("Member, Plant", "different Uuid concepts or identities")]
    [InlineData("Guid.NewGuid(), Owner", "generated values")]
    public void should_check_the_call_site_instead_of_only_the_behavior_parameter_types(string arguments, string reason)
    {
        Generate(UuidAggregateSources.With(arguments));
        Result.Source.ShouldNotContain("when ");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction && diagnostic.Message.Contains(reason, StringComparison.Ordinal)).ShouldBeTrue();
        AssertPartialDocument();
    }

    [Fact]
    public void should_refuse_an_optional_argument_behind_nullable_behavior_parameters()
    {
        Generate(UuidAggregateSources.With("Maybe, Owner", "Guid? left, Guid? right"));
        Result.Source.ShouldNotContain("when ");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction && diagnostic.Message.Contains("optional operands", StringComparison.Ordinal)).ShouldBeTrue();
        AssertPartialDocument();
    }

    [Fact]
    public void should_check_casts_in_the_behavior_as_well_as_the_call_site()
    {
        Generate(UuidAggregateSources.With("Maybe, Owner", "Guid? left, Guid right", "(Guid)left == right"));
        Result.Source.ShouldNotContain("when ");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction && diagnostic.Message.Contains("casts that change the operand type", StringComparison.Ordinal)).ShouldBeTrue();
        AssertPartialDocument();
    }

    [Fact]
    public void should_prove_the_bound_concept_literal_constructor()
    {
        Generate(UuidAggregateSources.With(
            "Owner, new MemberId(Guid.Parse(\"0f8fad5b-d9cb-469f-a165-70867728950e\"))",
            memberDefinition: "public record MemberId : ConceptAs<Guid> { public MemberId(Guid value) : base(Guid.Empty) { } }"));
        Result.Source.ShouldNotContain("when ");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction && diagnostic.Message.Contains("neither a canonical Uuid literal", StringComparison.Ordinal)).ShouldBeTrue();
        AssertPartialDocument();
    }
}
