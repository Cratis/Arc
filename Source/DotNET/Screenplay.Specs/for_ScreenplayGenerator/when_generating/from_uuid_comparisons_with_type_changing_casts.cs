// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_uuid_comparisons_with_type_changing_casts : a_generated_document
{
    [Theory]
    [InlineData("Owner == (Guid)Maybe")]
    [InlineData("(Guid)Maybe != Owner")]
    [InlineData("Owner == (Guid)(object)Maybe")]
    [InlineData("Member == (Guid)Plant")]
    public void should_refuse_the_cast_instead_of_emitting_a_comparison_against_the_unwrapped_property(string comparison)
    {
        Generate((Analyzed.SlicePath, UuidComparisonSources.Guard(comparison)));
        Result.Source.ShouldNotContain("when owner");
        Result.Source.ShouldNotContain("when maybe");
        Result.Source.ShouldNotContain("when member");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction && diagnostic.Message.Contains("casts that change the operand type", StringComparison.Ordinal)).ShouldBeTrue();
        AssertPartialDocument();
    }
}
