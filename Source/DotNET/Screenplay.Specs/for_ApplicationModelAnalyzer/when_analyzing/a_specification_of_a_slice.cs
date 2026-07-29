// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// The scenarios a slice is specified by sit in a folder beneath it, which is a namespace beneath its own, and are
/// written to a convention rigid enough to read: what had happened is stated against the scenario, the command is
/// issued through it, and each assertion says one thing about what followed. Reading them is what turns a document
/// stating what a slice does into one carrying the examples proving it.
/// </summary>
public class a_specification_of_a_slice : Specification
{
    const string Slice = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [ReadModel]
        public record Author(string Id, string Name);

        [Command]
        public record RegisterAuthor(string Name, int Age)
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

        public class and_the_author_is_new
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();

            void Establish()
            {
                _scenario.Given.ForEventSource("author").Events(new AuthorRegistered("Jane Austen"));
                _scenario.Given.ForEventSource("author").ReadModel(new Author("author", "Jane Austen"));
            }

            async Task Because() => await _scenario.Execute(new RegisterAuthor("Mary Shelley", 42));

            [Fact] void should_register_the_author() => _scenario.EventSequence.ShouldHaveAppendedEvent<AuthorRegistered>("author");
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", Slice),
        ("Library/Authors/Registration/when_registering/and_the_author_is_new.cs", Scenario),
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
    [Fact] void should_put_it_under_the_slice_it_specifies() => _analysis.Model.Slices.Single(_ => _.Name == "Registration").Specifications.Count().ShouldEqual(1);
    [Fact] void should_name_it_after_every_word_between_the_slice_and_itself() => _specification.Name.ShouldEqual("when_registering_and_the_author_is_new");
    [Fact] void should_start_from_the_event_that_had_happened() => _specification.Given.First().Name.ShouldEqual("AuthorRegistered");
    [Fact] void should_know_the_event_it_starts_from_is_an_event() => _specification.Given.First().Kind.ShouldEqual(SpecificationStateKind.Event);
    [Fact] void should_state_the_values_of_the_event_it_starts_from() => _specification.Given.First().Values.ShouldContainOnly([new PropertyMappingModel("Name", new LiteralSource("Jane Austen"))]);
    [Fact] void should_start_from_the_read_model_that_was_pinned() => _specification.Given.Last().Kind.ShouldEqual(SpecificationStateKind.ReadModel);
    [Fact] void should_state_the_values_of_the_read_model() => _specification.Given.Last().Values.Select(_ => _.Property).ShouldContainOnly(["Id", "Name"]);
    [Fact] void should_issue_the_command() => _specification.When.Name.ShouldEqual("RegisterAuthor");
    [Fact] void should_state_the_values_the_command_was_issued_with() => _specification.When.Values.ShouldContainOnly([new PropertyMappingModel("Name", new LiteralSource("Mary Shelley")), new PropertyMappingModel("Age", new LiteralSource(42))]);
    [Fact] void should_expect_the_event_the_assertion_names() => _specification.Then.Single().Name.ShouldEqual("AuthorRegistered");
    [Fact] void should_expect_no_rejection() => _specification.Errors.ShouldBeEmpty();
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
