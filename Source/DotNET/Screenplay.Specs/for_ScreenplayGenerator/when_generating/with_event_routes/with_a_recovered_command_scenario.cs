// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_event_routes;

public class with_a_recovered_command_scenario : a_generated_document
{
    void Because() => Generate(
        (Analyzed.SlicePath, StoredRouteScenarioSources.Command("stored-account", "stored-transactions")),
        ("Library/Authors/Registration/when_registering/and_it_succeeds.cs", StoredRouteScenarioSources.Scenario.Replace("GIVEN", string.Empty, StringComparison.Ordinal)),
        (IntegrationTesting.Path, IntegrationTesting.Source));

    [Fact] void should_keep_the_command_scenario() => Result.Source.ShouldContain("specification WhenRegisteringAndItSucceeds");
    [Fact] void should_declare_the_source_once() => Result.Source.Split("eventsource Account", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_declare_the_stream_once() => Result.Source.Split("stream Transactions", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_preserve_the_source_stored_name() => Result.Source.ShouldContain("id \"stored-account\"");
    [Fact] void should_preserve_the_stream_stored_name() => Result.Source.ShouldContain("id \"stored-transactions\"");
    [Fact] void should_compile_and_bind() => AssertDocument();
}
