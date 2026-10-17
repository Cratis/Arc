// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_authoring_only_constructs;

public class uuid_provisioning_guards : an_authoring_document
{
    void Because() => Generate(UuidComparisonSources.Provisioning("grant.Instance!.MemberId == RequestedBy"));

    [Fact] void should_recover_the_read_path_requirement() => Result.Source.ShouldContain("require grant.memberId == requestedBy\n            message \"Not allowed\"");
    [Fact] void should_compare_the_read_path_with_the_command_property() => Result.Source.ShouldContain("require grant.memberId == requestedBy");
    [Fact] void should_round_trip_with_only_the_existing_legacy_read_limit() => AssertAuthoringDocument(legacyReads: true);
}
