// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_uuid_arguments_to_an_aggregate_behavior : a_generated_document
{
    [Theory]
    [InlineData("Owner, Other", "when owner == other")]
    [InlineData("(Guid)(Owner), (Other)", "when owner == other")]
    [InlineData("Member, OtherMember", "when member == otherMember")]
    [InlineData("Member, Owner", "when member == owner")]
    [InlineData("Owner, Guid.Parse(\"0F8FAD5B-D9CB-469F-A165-70867728950E\")", "when owner == \"0f8fad5b-d9cb-469f-a165-70867728950e\"")]
    [InlineData("Guid.Empty, Owner", "when owner == \"00000000-0000-0000-0000-000000000000\"")]
    [InlineData("Member, new MemberId(new Guid(\"0f8fad5b-d9cb-469f-a165-70867728950e\"))", "when member == \"0f8fad5b-d9cb-469f-a165-70867728950e\"")]
    public void should_resolve_transparent_arguments_and_literals_in_the_callers_semantic_model(string arguments, string expected)
    {
        Generate(UuidAggregateSources.With(arguments));
        Result.Source.ShouldContain(expected);
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).ShouldBeFalse();
        AssertDocument();
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V28);
    }

    [Fact]
    public void should_keep_transparent_casts_in_the_behavior_admitted()
    {
        Generate(UuidAggregateSources.With("Owner, Other", comparison: "(Guid)(left) != right"));
        Result.Source.ShouldContain("when owner != other");
        AssertDocument();
    }
}
