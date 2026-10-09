// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_specification_event_routes : a_generated_document
{
    const string Scenario = """
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Authors.Registration;
        using Xunit;

        namespace Library.Authors.Registration.when_registering;

        public class and_it_succeeds
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            readonly EventSourceId _source = new("11111111-1111-1111-1111-111111111111");

            Task Establish() => _scenario.EventScenario.EventSequence.Append(_source, new AuthorRegistered("Prior"),
                eventStreamType: "Transactions", eventStreamId: "October", eventSourceType: "Account");

            Task Because() => _scenario.Execute(new RegisterAuthor("11111111-1111-1111-1111-111111111111", "October", "New"));

            [Fact] Task should_append() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>(_source, e => e.Name == "New");
        }
        """;

    static string Command => IdentifierSources.With("""
        [Command]
        [EventSourceType("Account")]
        [EventStreamType("Transactions")]
        public record RegisterAuthor([Key] string Id, string Month, string Name) : ICanProvideEventStreamId
        {
            public EventStreamId GetEventStreamId() => Month;
            public AuthorRegistered Handle() => new(Name);
        }
        """);

    [Fact]
    public void should_state_given_and_then_routes_only_in_authoring_mode()
    {
        GenerateScenario(true, Scenario);

        Result.Source.ShouldContain("given AuthorRegistered");
        Result.Source.ShouldContain("then AuthorRegistered");
        Result.Source.Split("stream Account.Transactions", StringSplitOptions.None).Length.ShouldEqual(4);
        Result.Source.Split("streamId = \"October\"", StringSplitOptions.None).Length.ShouldEqual(3);
        Result.Source.ShouldContain("for \"11111111-1111-1111-1111-111111111111\"");
        AssertAuthoringRoutes();
    }

    [Fact]
    public void should_withhold_a_routed_scenario_in_default_output()
    {
        GenerateScenario(false, Scenario);

        Result.Source.ShouldNotContain("specification WhenRegisteringAndItSucceeds");
        Result.Source.ShouldNotContain("stream Account.Transactions");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.SpecificationRouteNotRepresentable).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
        AssertDocument();
    }

    [Fact]
    public void should_report_an_unrouted_prior_occurrence_without_emitting_invalid_syntax()
    {
        var scenario = Scenario.Replace(",\n        eventStreamType: \"Transactions\", eventStreamId: \"October\", eventSourceType: \"Account\"", string.Empty, StringComparison.Ordinal);
        GenerateScenario(true, scenario);

        Result.Source.ShouldNotContain("no stream");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.SpecificationRouteNotRepresentable).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
        AssertAuthoringRoutes();
    }

    [Theory]
    [InlineData("")]
    [InlineData("e\u0301")]
    [InlineData(" October ")]
    public void should_not_guess_a_nonportable_route(string streamId)
    {
        GenerateScenario(true, Scenario.Replace("eventStreamId: \"October\"", $"eventStreamId: \"{streamId}\"", StringComparison.Ordinal));

        Result.Source.ShouldNotContain("specification WhenRegisteringAndItSucceeds");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeTrue();
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeFalse();
    }

    [Fact]
    public void should_not_key_a_declared_unkeyed_stream_from_a_specification()
    {
        var command = IdentifierSources.With("""
            [Command, EventSourceType("Account"), EventStreamType("Transactions")]
            public record RegisterAuthor([Key] string Id, string Month, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            """);
        GenerateScenario(true, Scenario, command);

        Result.Source.ShouldNotContain("specification WhenRegisteringAndItSucceeds");
        Result.Source.ShouldContain("eventsource Account");
        Result.Source.ShouldContain("stream Transactions");
        Result.Source.ShouldContain("stream Account.Transactions");
        Result.Source.ShouldNotContain("streamId");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeTrue();
        AssertAuthoringRoutes();
    }

    [Fact]
    public void should_not_declare_a_route_from_a_withheld_specification()
    {
        var scenario = Scenario.Replace("eventSourceType: \"Account\"", "eventSourceType: \"Discarded\"", StringComparison.Ordinal)
            .Replace("eventStreamId: \"October\"", "eventStreamId: \" October \"", StringComparison.Ordinal);
        GenerateScenario(true, scenario);

        Result.Source.ShouldNotContain("specification WhenRegisteringAndItSucceeds");
        Result.Source.ShouldNotContain("eventsource Discarded");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeTrue();
        AssertAuthoringRoutes();
    }

    void GenerateScenario(bool authoring, string scenario, string? command = null) => Generate(
        new ScreenplayOptions { AuthoringOnlyConstructs = authoring },
        (Analyzed.SlicePath, command ?? Command),
        ("Library/Feature/Slice/when_registering/and_it_succeeds.cs", scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source));

    void AssertAuthoringRoutes()
    {
        RoundTrip.Errors.ShouldBeEmpty();
        RoundTrip.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning).ShouldBeEmpty();
        RoundTrip.IsStable.ShouldBeTrue();
        Bound.Diagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error && !ExpectedBindingDiagnostics.IsExpected(diagnostic, true)).ShouldBeEmpty();
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeFalse();
    }
}
