// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_event_scenario_whose_producer_identifier_is_stripped : a_generated_document
{
    void Because() => Generate(
        (Analyzed.SlicePath, EventAppendSources.Producer.Replace("public AuthorRegistered Handle() => new(Name);", "public System.Threading.Tasks.Task Handle(Cratis.Chronicle.EventSequences.IEventLog log) => log.Append(EventSourceId.New(), new AuthorRegistered(Name));", StringComparison.Ordinal)),
        ("Library/Feature/Slice/when_appending/and_it_succeeds.cs", EventAppendSources.With("new EventSourceId(\"current\")", "new EventSourceId(\"current\")", "new EventSourceId(\"current\")")),
        (IntegrationTesting.Path, IntegrationTesting.Source));

    [Fact] void should_strip_the_identifier() => Result.Source.ShouldNotContain("identifier");
    [Fact] void should_keep_the_shared_source_implicit() => Result.Source.ShouldNotContain("for \"current\"");
    [Fact] void should_omit_the_append_only_scenario() => Result.Source.ShouldNotContain("specification");
    [Fact] void should_report_the_unrepresented_destination() => Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnrepresentableProductionDestination).ShouldEqual(1);
    [Fact] void should_compile_round_trip_and_bind() => AssertDocument();
}
