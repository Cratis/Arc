// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_event_scenario_with_a_non_guid_source_for_a_guid_key : a_generated_document
{
    void Because() => Generate(
        (Analyzed.SlicePath, EventAppendSources.Producer.Replace("[Key] string Id", "[Key] Guid Id", StringComparison.Ordinal)),
        ("Library/Feature/Slice/when_appending/and_it_succeeds.cs", EventAppendSources.With("new EventSourceId(\"current\")", "new EventSourceId(\"current\")", "new EventSourceId(\"current\")")),
        (IntegrationTesting.Path, IntegrationTesting.Source));

    [Fact] void should_keep_the_shared_source_implicit() => Result.Source.ShouldNotContain("for \"current\"");
    [Fact] void should_omit_the_append_only_scenario() => Result.Source.ShouldNotContain("specification");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
