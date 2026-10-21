// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_stating_concept_fixture_conversions;

public class with_a_transforming_operator : given.a_concept_fixture
{
    [Theory]
    [InlineData(false, "Urgent")]
    [InlineData(true, "Urgent")]
    [InlineData(false, "((ApprovalLabel)Urgent)!")]
    [InlineData(true, "((ApprovalLabel)Urgent)!")]
    public void should_omit_the_whole_scenario_and_explain_the_unproven_conversion(bool collection, string literal)
    {
        GenerateFixture(collection, conversion: "new(value.ToUpperInvariant())", literal: literal);
        AssertRefused();
    }
}
