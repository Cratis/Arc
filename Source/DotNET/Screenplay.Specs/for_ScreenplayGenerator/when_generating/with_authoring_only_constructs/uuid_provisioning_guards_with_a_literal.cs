// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class uuid_provisioning_guards_with_a_literal : an_authoring_document
{
    void Because() => Generate(UuidComparisonSources.Provisioning("grant.Instance!.Owner == Guid.Parse(\"0F8FAD5B-D9CB-469F-A165-70867728950E\")"));

    [Fact] void should_print_the_canonical_literal() => Result.Source.ShouldContain("== \"0f8fad5b-d9cb-469f-a165-70867728950e\"");
    [Fact] void should_round_trip_with_only_the_existing_legacy_read_limit() => AssertAuthoringDocument(legacyReads: true);
}
