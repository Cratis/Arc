// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// The event scenario a command scenario hands out is the one its command runs against, so seeding it says what had
/// happened - also when the scenario is held by a base context. An event scenario a specification creates for itself
/// is not what the command runs against, so a scenario seeding one cannot say what had happened and is left out with
/// a warning rather than written without it.
/// </summary>
public class from_source_seeding_an_event_log_a_command_scenario_does_not_hand_out : Specification
{
    const string Slice = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Customers.Onboarding;

        [EventType]
        public record CustomerRegistered(string Name);

        [Command]
        public record StartOnboarding(string OrgName)
        {
            public CustomerRegistered Handle() => new(OrgName);
        }
        """;

    const string Base = """
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Library.Customers.Onboarding;

        namespace Library.Customers.Onboarding.when_starting_onboarding.given;

        public class a_started_onboarding
        {
            protected CommandScenario<StartOnboarding> Scenario = new();
            protected Result Result = null!;
        }
        """;

    const string Inherited = """
        using System.Threading.Tasks;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Customers.Onboarding;
        using Library.Customers.Onboarding.when_starting_onboarding.given;
        using Xunit;

        namespace Library.Customers.Onboarding.when_starting_onboarding;

        public class and_the_scenario_comes_from_a_base_context : a_started_onboarding
        {
            async Task Establish() =>
                await Scenario.EventScenario.Given.ForEventSource(EventSourceId.New()).Events(new CustomerRegistered("Existing Customer AS"));

            async Task Because() => Result = await Scenario.Execute(new StartOnboarding("Racing Customer AS"));

            [Fact] void should_not_succeed() => Result.ShouldNotBeSuccessful();
            [Fact] async Task should_only_hold_the_seeded_registration() =>
                await Scenario.EventScenario.EventSequence.ShouldHaveAppendedEvent<CustomerRegistered>(
                    EventSourceId.New(), @event => @event.Name == "Existing Customer AS");
        }
        """;

    const string Separate = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Customers.Onboarding;
        using Xunit;

        namespace Library.Customers.Onboarding.when_starting_onboarding;

        public class and_the_log_seeded_is_the_specifications_own
        {
            readonly EventScenario _log = new();
            readonly CommandScenario<StartOnboarding> _scenario = new();
            Result _result = null!;

            async Task Establish() =>
                await _log.Given.ForEventSource(EventSourceId.New()).Events(new CustomerRegistered("Existing Customer AS"));

            async Task Because() => _result = await _scenario.Execute(new StartOnboarding("Racing Customer AS"));

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Customers/Onboarding/Onboarding.cs", Slice),
        ("Library/Customers/Onboarding/when_starting_onboarding/given/a_started_onboarding.cs", Base),
        ("Library/Customers/Onboarding/when_starting_onboarding/and_the_scenario_comes_from_a_base_context.cs", Inherited),
        ("Library/Customers/Onboarding/when_starting_onboarding/and_the_log_seeded_is_the_specifications_own.cs", Separate),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];

    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;

    void Because()
    {
        _result = new ScreenplayGenerator().Generate(Analyzed.Compile(_sources), new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
    }

    IEnumerable<string> Lines() =>
        _result.Source.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(_ => _.Trim());

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_state_only_the_scenario_it_can_read() => Lines().Count(_ => _.StartsWith("specification ", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_state_the_seeded_event_as_given() => Lines().ShouldContain("given CustomerRegistered");
    [Fact] void should_not_state_the_seeded_event_as_an_outcome() => Lines().ShouldNotContain("then CustomerRegistered");
    [Fact] void should_report_the_scenario_it_left_out() => _result.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldEqual(1);
    [Fact] void should_warn_about_it() => _result.Diagnostics
        .Where(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecification)
        .All(_ => _.Severity == ScreenplayDiagnosticSeverity.Warning)
        .ShouldBeTrue();
    [Fact] void should_say_why_it_left_the_scenario_out() => _result.Diagnostics
        .Where(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecification)
        .Select(_ => _.Message)
        .ShouldContain("The scenario 'when_starting_onboarding_and_the_log_seeded_is_the_specifications_own' was left out because it seeds an event log that this cannot tell the command it issues runs against");
}
