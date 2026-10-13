// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.with_event_routes;

public class with_conflicting_specification_stored_names : a_generated_document
{
    [Theory]
    [InlineData("stored-account", "Transactions")]
    [InlineData("Account", "stored-transactions")]
    [InlineData("stored-account", "stored-transactions")]
    void should_refuse_a_specification_naming_another_stored_destination(string storedSource, string storedStream)
    {
        const string Given = """
            Task Establish() => _scenario.EventScenario.EventSequence.Append("other", new AuthorRegistered("Prior"),
                eventSourceType: "Account", eventStreamType: "Transactions");
            """;
        var scenario = StoredRouteScenarioSources.Scenario.Replace("GIVEN", Given, StringComparison.Ordinal);
        Generate(
            (Analyzed.SlicePath, StoredRouteScenarioSources.Command(storedSource, storedStream)),
            ("Library/Authors/Registration/when_registering/and_it_succeeds.cs", scenario),
            (IntegrationTesting.Path, IntegrationTesting.Source));

        Result.Source.ShouldNotContain("specification WhenRegisteringAndItSucceeds");
        Result.Source.Split("eventsource Account", StringSplitOptions.None).Length.ShouldEqual(2);
        Result.Source.Split("stream Transactions", StringSplitOptions.None).Length.ShouldEqual(2);
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeTrue();
        AssertDocument();
    }
}
