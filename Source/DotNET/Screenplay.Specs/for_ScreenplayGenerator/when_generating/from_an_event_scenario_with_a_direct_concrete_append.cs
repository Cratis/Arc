// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_event_scenario_with_a_direct_concrete_append : a_generated_document
{
    void Because() => Generate(
        (Analyzed.SlicePath, EventAppendSources.Producer),
        ("Library/Feature/Slice/when_appending/and_it_succeeds.cs", EventAppendSources.With("new EventSourceId(\"prior\")", "new EventSourceId(\"current\")", "new EventSourceId(\"current\")", true)),
        (IntegrationTesting.Path, IntegrationTesting.Source));

    [Fact] void should_state_the_prior_source() => Result.Source.ShouldContain("given AuthorRegistered\n          for \"prior\"");
    [Fact] void should_state_the_append_source() => Result.Source.ShouldContain("when append AuthorRegistered\n          for \"current\"");
    [Fact] void should_state_the_asserted_source() => Result.Source.ShouldContain("then AuthorRegistered\n          for \"current\"");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
