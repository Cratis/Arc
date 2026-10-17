// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_protected_reads;

public class from_uuid_comparisons_between_reads : a_uuid_read_document
{
    [Theory]
    [InlineData("grant.Owner == alternative.Owner", "require grant.owner == alternative.owner")]
    [InlineData("grant.Owner != alternative.Owner", "require grant.owner != alternative.owner")]
    [InlineData("grant.Owner == (Guid)alternative.Owner", "require grant.owner == alternative.owner")]
    public void should_bind_and_round_trip_the_comparison_of_two_protected_reads(string comparison, string expected)
    {
        GenerateComparison(comparison);
        Result.Source.ShouldContain(expected);
        Authoring.Source.ShouldContain(expected);
        Result.Source.ShouldContain("reads RoomGrant as grant");
        Result.Source.ShouldContain("reads RoomGrant as alternative");
        Result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
        Bound.Success.ShouldBeTrue();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V28);
        RoundTrip.IsStable.ShouldBeTrue();
    }
}
