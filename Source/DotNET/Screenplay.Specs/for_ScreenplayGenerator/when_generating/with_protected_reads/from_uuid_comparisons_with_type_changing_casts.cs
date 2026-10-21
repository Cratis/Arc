// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_uuid_comparisons_with_type_changing_casts : a_uuid_read_document
{
    [Theory]
    [InlineData("grant.Owner == (Guid)Maybe")]
    [InlineData("Requester == (Guid)grant.Optional")]
    [InlineData("(Guid)grant.Optional != Requester")]
    public void should_leave_the_nontransparent_cast_in_code(string comparison)
    {
        GenerateComparison(comparison);
        Result.Source.ShouldNotContain("require ");
        Result.Source.ShouldContain("reads RoomGrant as grant");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction && diagnostic.Message.Contains("casts that change the operand type", StringComparison.Ordinal)).ShouldBeTrue();
        Result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
        Bound.Success.ShouldBeTrue();
        RoundTrip.IsStable.ShouldBeTrue();
    }
}
