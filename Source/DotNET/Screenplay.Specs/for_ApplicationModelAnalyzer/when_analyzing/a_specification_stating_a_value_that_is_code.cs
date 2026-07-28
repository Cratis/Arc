// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A scenario written in the host language routinely rests an identity on a value made at run time, which is exactly
/// what a document stating values has no way to name. Unlike a step, a value stands on its own: leaving one out
/// leaves the rest of the scenario saying what the source says, so it is left out and said rather than taking the
/// scenario with it.
/// </summary>
public class a_specification_stating_a_value_that_is_code : Specification
{
    const string Slice = """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [Command]
        public record RegisterAuthor(Guid Id, string Name, int Age)
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

        public class and_the_identity_is_made_at_run_time
        {
            readonly CommandScenario<RegisterAuthor> _scenario = new();
            readonly Guid _id = Guid.NewGuid();
            Result _result = null!;

            async Task Because() => _result = await _scenario.Execute(new RegisterAuthor(_id, "Jane Austen", 41));

            [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Authors/Registration/Registration.cs", Slice),
        ("Library/Authors/Registration/when_registering/and_the_identity_is_made_at_run_time.cs", Scenario),
        (IntegrationTesting.Path, IntegrationTesting.Source)
    ];

    ApplicationModelAnalysis _analysis;
    SpecificationModel _specification;

    void Establish()
    {
        _analysis = Analyzed.Source(_sources);
        _specification = _analysis.Model.Slices.Single(_ => _.Name == "Registration").Specifications.Single();
    }

    ScreenplayDiagnostic LeftOut() =>
        _analysis.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UnreadableSpecificationValue);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_keep_the_scenario() => _specification.When.Name.ShouldEqual("RegisterAuthor");
    [Fact] void should_state_every_value_it_can_read() => _specification.When.Values.ShouldContainOnly([new PropertyMappingModel("Name", new LiteralSource("Jane Austen")), new PropertyMappingModel("Age", new LiteralSource(41))]);
    [Fact] void should_leave_out_the_one_it_cannot() => _specification.When.Values.Select(_ => _.Property).ShouldNotContain("Id");
    [Fact] void should_say_which_value_it_left_out() => LeftOut().Message.ShouldEqual("The value 'when_registering_and_the_identity_is_made_at_run_time' states for 'RegisterAuthor.Id' is code rather than a constant, so the scenario states everything but that value");
    [Fact] void should_say_where_it_left_it_out() => LeftOut().Location.ShouldEqual("Library.Authors.Registration.when_registering.and_the_identity_is_made_at_run_time");
    [Fact] void should_report_it_as_information() => LeftOut().Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
}
