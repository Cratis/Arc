// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// An identity made where it is stated has no value to state - nobody wrote one down - so a document leaving it out
/// says exactly as much as the source does. Reporting it would name a difference between the two that is not there,
/// which is the opposite of what reporting a value that was left out is for.
/// </summary>
public class a_specification_making_an_identity_where_it_states_it : Specification
{
    const string Slice = """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [Command]
        public record RegisterAuthor(Guid Id, string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    const string Scenario = """
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.Testing.EventSequences;
        using Library.Authors.Registration;
        using Xunit;

        namespace Library.Authors.Registration.when_registering;

        public class and_the_identity_is_made_on_the_spot
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new RegisterAuthor(Guid.NewGuid(), "Jane Austen"));

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", Slice),
        ("Library/Authors/Registration/when_registering/and_the_identity_is_made_on_the_spot.cs", Scenario),
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
    [Fact] void should_keep_the_scenario() => _specification.When.Name.ShouldEqual("RegisterAuthor");
    [Fact] void should_state_every_value_it_can_read() => _specification.When.Values.ShouldContainOnly([new PropertyMappingModel("Name", new LiteralSource("Jane Austen"))]);
    [Fact] void should_leave_out_the_identity() => _specification.When.Values.Select(_ => _.Property).ShouldNotContain("Id");
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
