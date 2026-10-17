// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_unadmitted_uuid_conditions : a_generated_document
{
    [Theory]
    [InlineData("Member == Plant", "different Uuid concepts or identities")]
    [InlineData("Plant != Member", "different Uuid concepts or identities")]
    [InlineData("Owner < Other", "only == and != are admitted")]
    [InlineData("Member >= OtherMember", "only == and != are admitted")]
    [InlineData("Owner == Maybe", "optional operands are not admitted")]
    [InlineData("Owner == Guid.NewGuid()", "generated values such as Guid.NewGuid() are not admitted")]
    [InlineData("Owner == Guid.Parse(\"not-a-guid\")", "neither a canonical Uuid literal")]
    [InlineData("Owner == Guid.Parse(Text)", "neither a canonical Uuid literal")]
    public void should_report_the_property_and_refusal_without_emitting_an_invalid_condition(string comparison, string reason)
    {
        Generate((Analyzed.SlicePath, UuidComparisonSources.Guard(comparison)));
        Result.Source.ShouldNotContain("when owner");
        Result.Source.ShouldNotContain("when member");
        Result.Source.ShouldNotContain("when plant");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction && diagnostic.Message.Contains(reason, StringComparison.Ordinal)).ShouldBeTrue();
        AssertPartialDocument();
    }
}
