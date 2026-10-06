// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_event_scenario_with_conflicting_producer_identifier_types : a_generated_document
{
    void Because() => Generate(
        (Analyzed.SlicePath, EventAppendSources.Producer + """
            [Command]
            public record RegisterAnotherAuthor([Key] Guid Id, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            """),
        ("Library/Feature/Slice/when_appending/and_it_succeeds.cs", EventAppendSources.With("new EventSourceId(\"11111111-1111-1111-1111-111111111111\")", "new EventSourceId(\"11111111-1111-1111-1111-111111111111\")", "new EventSourceId(\"11111111-1111-1111-1111-111111111111\")")),
        (IntegrationTesting.Path, IntegrationTesting.Source));

    [Fact] void should_keep_the_shared_source_implicit() => Result.Source.ShouldNotContain("for \"11111111-1111-1111-1111-111111111111\"");
    [Fact] void should_keep_the_scenario() => Result.Source.ShouldContain("specification");
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
