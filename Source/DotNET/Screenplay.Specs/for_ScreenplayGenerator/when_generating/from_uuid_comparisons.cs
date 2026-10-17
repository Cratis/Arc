// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_uuid_comparisons : a_generated_document
{
    [Theory]
    [InlineData("Owner == Other", "when owner == other")]
    [InlineData("Owner != Other", "when owner != other")]
    [InlineData("Member == OtherMember", "when member == otherMember")]
    [InlineData("Member != OtherMember", "when member != otherMember")]
    [InlineData("Member == Owner", "when member == owner")]
    [InlineData("Owner == Member", "when owner == member")]
    [InlineData("Plant == Owner", "when plant == owner")]
    public void should_bind_and_round_trip_property_comparisons(string comparison, string expected)
    {
        Generate((Analyzed.SlicePath, UuidComparisonSources.Guard(comparison)));
        Result.Source.ShouldContain(expected);
        AssertDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V28);
    }

    [Theory]
    [InlineData("Owner == Guid.Parse(\"0F8FAD5B-D9CB-469F-A165-70867728950E\")", "when owner == \"0f8fad5b-d9cb-469f-a165-70867728950e\"")]
    [InlineData("Owner != new Guid(\"0f8fad5b-d9cb-469f-a165-70867728950e\")", "when owner != \"0f8fad5b-d9cb-469f-a165-70867728950e\"")]
    [InlineData("Guid.Parse(\"0f8fad5b-d9cb-469f-a165-70867728950e\") == Owner", "when owner == \"0f8fad5b-d9cb-469f-a165-70867728950e\"")]
    [InlineData("Member == new MemberId(Guid.Parse(\"0f8fad5b-d9cb-469f-a165-70867728950e\"))", "when member == \"0f8fad5b-d9cb-469f-a165-70867728950e\"")]
    [InlineData("Member == Guid.Parse(\"{0f8fad5b-d9cb-469f-a165-70867728950e}\")", "when member == \"0f8fad5b-d9cb-469f-a165-70867728950e\"")]
    [InlineData("Plant == new PlantId(new Guid(\"0F8FAD5B-D9CB-469F-A165-70867728950E\"))", "when plant == \"0f8fad5b-d9cb-469f-a165-70867728950e\"")]
    [InlineData("Owner == Guid.Empty", "when owner == \"00000000-0000-0000-0000-000000000000\"")]
    public void should_print_literals_in_canonical_lowercase_form(string comparison, string expected)
    {
        Generate((Analyzed.SlicePath, UuidComparisonSources.Guard(comparison)));
        Result.Source.ShouldContain(expected);
        AssertDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V28);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_respect_the_version_cap_in_both_modes(bool authoring)
    {
        Generate(new ScreenplayOptions { MaximumExecutableModelVersion = SemanticVersion.V27, AuthoringOnlyConstructs = authoring }, (Analyzed.SlicePath, UuidComparisonSources.Guard("Member == OtherMember")));
        Result.Source.ShouldNotContain("when member == otherMember");
        Result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("ESM v28", StringComparison.Ordinal) && diagnostic.Message.Contains("member", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
        AssertPartialDocument();
    }

    [Fact]
    public void should_withhold_a_protected_read_requirement_below_the_cap()
    {
        Generate(new ScreenplayOptions { MaximumExecutableModelVersion = SemanticVersion.V27, AuthoringOnlyConstructs = true }, (Analyzed.SlicePath, UuidComparisonSources.Provisioning("grant.Instance!.MemberId == RequestedBy")));
        Result.Source.ShouldNotContain("require grant.memberId");
        Result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("ESM v28", StringComparison.Ordinal) && diagnostic.Message.Contains("grant.MemberId", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    }
}
