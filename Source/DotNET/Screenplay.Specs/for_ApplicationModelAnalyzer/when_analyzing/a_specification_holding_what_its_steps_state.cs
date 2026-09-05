// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A specification routinely puts what it starts from and what it issues into a member and names that member in the
/// step, rather than constructing both where the step is written - the same value is stated once and asserted on
/// later, or the command is built where the values it needs already are. It says exactly what the inline shape says,
/// so it has to read as the same scenario; reading only what is written inline leaves those out whole.
/// </summary>
public class a_specification_holding_what_its_steps_state : Specification
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

        public class and_the_name_is_already_taken
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            AuthorRegistered _registered = null!;
            RegisterAuthor _command = null!;
            Result _result = null!;

            void Establish()
            {
                _registered = new AuthorRegistered("Jane Austen");
                _command = new RegisterAuthor("Jane Austen");
                _scenario.Given.ForEventSource("author").Events(_registered);
            }

            async Task Because() => _result = await _scenario.Execute(_command);

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", Slice),
        ("Library/Authors/Registration/when_registering/and_the_name_is_already_taken.cs", Scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];

    ApplicationModelAnalysis _analysis;
    SpecificationModel _specification;

    void Establish()
    {
        _analysis = Analyzed.Source(_sources);
        _specification = _analysis.Model.Slices.Single(_ => _.Name == "Registration").Specifications.Single();
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_read_what_it_starts_from_through_the_member_holding_it() => _specification.Given.Single().Name.ShouldEqual("AuthorRegistered");
    [Fact] void should_state_the_values_that_starting_point_was_put_together_with() => _specification.Given.Single().Values.ShouldContainOnly([new PropertyMappingModel("Name", new LiteralSource("Jane Austen"))]);
    [Fact] void should_read_the_command_through_the_member_holding_it() => _specification.When.Name.ShouldEqual("RegisterAuthor");
    [Fact] void should_state_the_values_the_command_was_put_together_with() => _specification.When.Values.ShouldContainOnly([new PropertyMappingModel("Name", new LiteralSource("Jane Austen"))]);
    [Fact] void should_read_the_rejection_it_expects() => _specification.Errors.ShouldNotBeEmpty();
    [Fact] void should_expect_no_event_alongside_that_rejection() => _specification.Then.ShouldBeEmpty();
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
