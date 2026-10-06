// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_event_scenario_with_guid_sources_for_a_typed_key : a_generated_document
{
    void Because() => Generate(
        (Analyzed.SlicePath, EventAppendSources.Producer.Replace("[Key] string Id", "AuthorId Id", StringComparison.Ordinal)),
        ("Library/Feature/Slice/when_appending/and_it_succeeds.cs", EventAppendSources.With("new EventSourceId(\"11111111-1111-1111-1111-111111111111\")", "new EventSourceId(\"22222222-2222-2222-2222-222222222222\")", "new EventSourceId(\"22222222-2222-2222-2222-222222222222\")")),
        (IntegrationTesting.Path, IntegrationTesting.Source));

    [Fact] void should_state_the_prior_source() => Result.Source.ShouldContain("given AuthorRegistered\n          for \"11111111-1111-1111-1111-111111111111\"");
    [Fact] void should_state_the_append_source() => Result.Source.ShouldContain("when append AuthorRegistered\n          for \"22222222-2222-2222-2222-222222222222\"");
    [Fact] void should_state_the_asserted_source() => Result.Source.ShouldContain("then AuthorRegistered\n          for \"22222222-2222-2222-2222-222222222222\"");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
