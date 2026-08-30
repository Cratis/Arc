// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A specification driving a read model issues no command - the events are what happened, and what followed is the
/// model they built. That is the two halves the language already holds, so it is read as one: given the events, then
/// the read model the scenario says it is of. Where the events are written differs from a specification of a command,
/// which sets its world up and then acts: here the events are the action, and are routinely written straight into it.
/// </summary>
public class a_specification_of_a_read_model : Specification
{
    const string Slice = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Library.Authors.Listing;

        [EventType]
        public record AuthorRegistered(string Name);

        [EventType]
        public record AuthorArchived();

        [ReadModel]
        [FromEvent<AuthorRegistered>]
        public record Author
        {
            public string Id { get; init; } = string.Empty;

            public static IEnumerable<Author> All() => [];
        }
        """;

    const string Scenario = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.ReadModels;
        using Cratis.Specifications;
        using Xunit;

        namespace Library.Authors.Listing.when_an_author_is_archived;

        public class and_the_author_was_registered_first : Specification
        {
            readonly ReadModelScenario<Author> _scenario = new();

            async Task Establish() =>
                await _scenario.Given
                    .ForEventSource(EventSourceId.New())
                    .Events(new AuthorRegistered("Jane Austen"), new AuthorArchived());

            [Fact] void should_hold_the_author() => _scenario.Instance!.Id.ShouldEqual("");
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Listing/Listing.cs", Slice),
        ("Library/Authors/Listing/when_an_author_is_archived/and_the_author_was_registered_first.cs", Scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];

    ApplicationModelAnalysis _analysis;
    SpecificationModel _specification;

    void Establish()
    {
        _analysis = Analyzed.Source(_sources);
        _specification = _analysis.Model.Slices.Single(_ => _.Name == "Listing").Specifications.Single();
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_state_what_had_happened() => _specification.Given.Select(_ => _.Name).ShouldContainOnly(["AuthorArchived", "AuthorRegistered"]);
    [Fact] void should_read_the_events_from_where_the_action_is() => _specification.Given.Count().ShouldEqual(2);
    [Fact] void should_state_them_as_events() => _specification.Given.All(_ => _.Kind == SpecificationStateKind.Event).ShouldBeTrue();
    [Fact] void should_issue_no_command() => _specification.When.ShouldBeNull();
    [Fact] void should_say_the_read_model_followed() => _specification.Then.Single().Name.ShouldEqual("Author");
    [Fact] void should_state_it_as_a_read_model() => _specification.Then.Single().Kind.ShouldEqual(SpecificationStateKind.ReadModel);
    [Fact] void should_state_every_exact_read_model_value() => _specification.Then.Single().Values.ShouldContainOnly([new PropertyMappingModel("Id", new LiteralSource(string.Empty))]);
    [Fact] void should_retain_the_exact_read_model_symbol() => SpecificationEvidence.For(_specification).States[_specification.Then.Single()].Artifact.Name.ShouldEqual("Author");
    [Fact] void should_retain_read_model_step_evidence() => SpecificationEvidence.For(_specification).States[_specification.Then.Single()].Source.IsInSource.ShouldBeTrue();
    [Fact] void should_report_no_scenario_left_out() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeFalse();
    [Fact] void should_report_no_scenario_without_a_counterpart() => _analysis.Diagnostics.Any(_ => _.Code == ScreenplayDiagnosticCodes.ScenarioWithoutCounterpart).ShouldBeFalse();
}
