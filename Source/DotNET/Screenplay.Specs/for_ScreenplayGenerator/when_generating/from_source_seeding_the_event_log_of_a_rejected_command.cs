// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A scenario proving a duplicate is turned down seeds the event log of the command scenario and then asserts the log
/// still holds only what was seeded. The seeded event is something that had already happened, so it is a given - and
/// the assertion that it is still there says nothing a rejected command produced, so it is no outcome. A command
/// that goes through and appends an event of the same kind as one seeded still produces that event.
/// </summary>
public class from_source_seeding_the_event_log_of_a_rejected_command : Specification
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

    const string Rejected = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Customers.Onboarding;
        using Xunit;

        namespace Library.Customers.Onboarding.when_starting_onboarding;

        public class and_a_duplicate_onboarding_races
        {
            readonly EventSourceId _orgNumber = "918164529";
            CommandScenario<StartOnboarding> _scenario = null!;
            Result _result = null!;

            async Task Establish()
            {
                _scenario = new CommandScenario<StartOnboarding>();
                await _scenario.EventScenario.Given
                    .ForEventSource(_orgNumber)
                    .Events(new CustomerRegistered("Existing Customer AS"));
            }

            async Task Because() => _result = await _scenario.Execute(new StartOnboarding("Racing Customer AS"));

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
            [Fact] async Task should_only_hold_the_seeded_registration() =>
                await _scenario.EventScenario.EventSequence.ShouldHaveAppendedEvent<CustomerRegistered>(
                    _orgNumber, @event => @event.Name == "Existing Customer AS");
        }
        """;

    const string Accepted = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Customers.Onboarding;
        using Xunit;

        namespace Library.Customers.Onboarding.when_starting_onboarding;

        public class and_another_customer_registered_earlier
        {
            readonly EventSourceId _orgNumber = "918164529";
            CommandScenario<StartOnboarding> _scenario = null!;
            Result _result = null!;

            async Task Establish()
            {
                _scenario = new CommandScenario<StartOnboarding>();
                await _scenario.EventScenario.Given
                    .ForEventSource(_orgNumber)
                    .Events(new CustomerRegistered("Earlier Customer AS"));
            }

            async Task Because() => _result = await _scenario.Execute(new StartOnboarding("Later Customer AS"));

            [Fact] async Task should_register_the_customer() =>
                await _scenario.EventScenario.EventSequence.ShouldHaveAppendedEvent<CustomerRegistered>(
                    _orgNumber, @event => @event.Name == "Later Customer AS");
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Customers/Onboarding/Onboarding.cs", Slice),
        ("Library/Customers/Onboarding/when_starting_onboarding/and_a_duplicate_onboarding_races.cs", Rejected),
        ("Library/Customers/Onboarding/when_starting_onboarding/and_another_customer_registered_earlier.cs", Accepted),
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

    IEnumerable<string> Block(string name) =>
        Lines().SkipWhile(_ => !_.StartsWith($"specification {name}", StringComparison.Ordinal)).Skip(1)
            .TakeWhile(_ => !_.StartsWith("specification ", StringComparison.Ordinal));

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_state_both_scenarios() => Lines().Count(_ => _.StartsWith("specification ", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_state_the_seeded_event_as_given() => Block("WhenStartingOnboardingAndADuplicateOnboardingRaces").ShouldContain("given CustomerRegistered");
    [Fact] void should_state_the_seeded_values() => Block("WhenStartingOnboardingAndADuplicateOnboardingRaces").ShouldContain(@"name = ""Existing Customer AS""");
    [Fact] void should_state_the_rejection() => Block("WhenStartingOnboardingAndADuplicateOnboardingRaces").Any(_ => _.StartsWith("then error", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_state_the_seeded_event_as_an_outcome() => Block("WhenStartingOnboardingAndADuplicateOnboardingRaces").ShouldNotContain("then CustomerRegistered");
    [Fact] void should_state_the_event_a_successful_command_appends() => Block("WhenStartingOnboardingAndAnotherCustomerRegisteredEarlier").ShouldContain("then CustomerRegistered");
    [Fact] void should_report_nothing_left_out() => _result.Diagnostics.Where(_ => _.Severity != ScreenplayDiagnosticSeverity.Information).ShouldBeEmpty();
}
