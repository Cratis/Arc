// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_uuid_fixture_in_a_strategy_document : a_pinned_uuid_value
{
    [Theory]
    [InlineData("aaaaaaaa-bbbb-1ccc-8ddd-eeeeeeeeeeee")]
    [InlineData("aaaaaaaa-bbbb-4ccc-7ddd-eeeeeeeeeeee")]
    public void should_withhold_a_uuid_pin_outside_the_v15_shape(string pin)
    {
        GeneratePin(pin, new(), NanoId);
        Result.Source.ShouldContain("generate nanoid");
        Result.Source.ShouldContain("receipt ReceiptId generated");
        Result.Source.ShouldNotContain("specification WhenAllocatingAndAReceiptIsPinned");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("version 4 with the RFC variant");
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V15);
        AssertDocument();
    }

    [Fact]
    void should_keep_a_version_four_fixture()
    {
        GeneratePin("AAAAAAAA-BBBB-4CCC-8DDD-EEEEEEEEEEEE", new(), NanoId);
        Result.Source.ShouldContain("generated receipt = \"aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee\"");
        AssertDocument();
        Run("WhenAllocatingAndAReceiptIsPinned").Passed.ShouldBeTrue();
    }

    [Fact]
    void should_keep_legacy_guid_acceptance_without_non_default_generation()
    {
        GeneratePin("aaaaaaaa-bbbb-1ccc-7ddd-eeeeeeeeeeee", new());
        Result.Source.ShouldContain("specification WhenAllocatingAndAReceiptIsPinned");
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
        AssertDocument();
        Run("WhenAllocatingAndAReceiptIsPinned").Passed.ShouldBeTrue();
    }

    [Fact]
    void should_keep_legacy_guid_acceptance_below_the_strategy_cap()
    {
        GeneratePin("aaaaaaaa-bbbb-1ccc-7ddd-eeeeeeeeeeee", new() { MaximumExecutableModelVersion = SemanticVersion.V14 }, NanoId);
        Result.Source.ShouldNotContain("generate nanoid");
        Result.Source.ShouldContain("specification WhenAllocatingAndAReceiptIsPinned");
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
        AssertDocument();
        Run("WhenAllocatingAndAReceiptIsPinned").Passed.ShouldBeTrue();
    }

    [Fact]
    void should_not_raise_fixture_requirements_for_a_withheld_strategy()
    {
        GeneratePin("aaaaaaaa-bbbb-1ccc-7ddd-eeeeeeeeeeee", new(), NanoId + CodeRules);
        Result.Source.ShouldNotContain("generate nanoid");
        Result.Source.ShouldContain("specification WhenAllocatingAndAReceiptIsPinned");
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
        AssertDocument();
        Run("WhenAllocatingAndAReceiptIsPinned").Passed.ShouldBeTrue();
    }
}
