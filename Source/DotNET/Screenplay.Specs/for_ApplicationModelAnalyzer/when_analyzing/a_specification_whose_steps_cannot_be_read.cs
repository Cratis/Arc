// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A scenario is one example, and an example missing the state it started from, the command it issued or the outcome
/// it expects is a different example from the one the source states. Each of the three is left out whole and said
/// so, which is the difference between a document with a known gap and one that is quietly wrong about what an
/// application was specified against.
/// <para>
/// A value the specification holds is followed to where it was put together, but only when it was put together in
/// one place. Given twice, it held different values in different runs and the source does not say which one the step
/// saw - so it stays unread, the same as a step written under a condition.
/// </para>
/// </summary>
public class a_specification_whose_steps_cannot_be_read : Specification
{
    const string Slice = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [Command]
        public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Authors.Registration;
        using Xunit;

        namespace Library.Authors.Registration.when_registering;

        public class and_the_command_is_held_in_a_field_given_twice
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            RegisterAuthor _command = new("Jane Austen");
            Result _result = null!;

            void Establish() => _command = new RegisterAuthor("Mary Shelley");

            async Task Because() => _result = await _scenario.Execute(_command);

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
        }

        public class and_what_it_starts_from_depends_on_a_condition
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            Result _result = null!;

            protected virtual bool AuthorAlreadyRegistered => true;

            void Establish()
            {
                if (AuthorAlreadyRegistered)
                {
                    _scenario.Given.ForEventSource("author").Events(new AuthorRegistered("Jane Austen"));
                }
            }

            async Task Because() => _result = await _scenario.Execute(new RegisterAuthor("Mary Shelley"));

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
        }

        public class and_nothing_it_expects_has_a_place_in_the_language
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new RegisterAuthor("Mary Shelley"));

            [Fact] void should_succeed() => _result.ShouldBeSuccessful();
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", Slice),
        ("Library/Authors/Registration/when_registering/and_the_command_is_held_in_a_field_given_twice.cs", Scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(_sources);

    IEnumerable<ScreenplayDiagnostic> LeftOut() =>
        _analysis.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecification);

    bool SaidOf(string scenario, string reason) =>
        LeftOut().Any(_ => _.Message.Contains(scenario, StringComparison.Ordinal) && _.Message.Contains(reason, StringComparison.Ordinal));

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_leave_out_every_scenario_it_cannot_read() => _analysis.Model.Slices.Single(_ => _.Name == "Registration").Specifications.ShouldBeEmpty();
    [Fact] void should_report_each_of_them() => LeftOut().Count().ShouldEqual(3);
    [Fact] void should_say_a_command_put_together_elsewhere_cannot_be_read() => SaidOf("and_the_command_is_held_in_a_field_given_twice", "the command it issues is put together somewhere this cannot read").ShouldBeTrue();
    [Fact] void should_say_a_starting_point_under_a_condition_cannot_be_read() => SaidOf("and_what_it_starts_from_depends_on_a_condition", "only happens under a condition").ShouldBeTrue();
    [Fact] void should_say_an_outcome_the_language_cannot_hold_cannot_be_read() => SaidOf("and_nothing_it_expects_has_a_place_in_the_language", "it expects no event and no rejection").ShouldBeTrue();
    [Fact] void should_say_where_each_of_them_lives() => LeftOut().Select(_ => _.Location).ShouldContain("Library.Authors.Registration.when_registering.and_the_command_is_held_in_a_field_given_twice");
    [Fact] void should_report_them_as_warnings() => LeftOut().Select(_ => _.Severity).ShouldContainOnly([ScreenplayDiagnosticSeverity.Warning, ScreenplayDiagnosticSeverity.Warning, ScreenplayDiagnosticSeverity.Warning]);
}
