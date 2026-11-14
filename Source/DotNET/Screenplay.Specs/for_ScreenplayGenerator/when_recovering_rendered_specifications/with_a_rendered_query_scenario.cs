// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_rendered_specifications;

public class with_a_rendered_query_scenario : given.a_rendered_query_scenario
{
    SpecificationModel _specification = null!;
    SpecificationQuerySyntax _query = null!;

    void Because()
    {
        GenerateScenario(given.RenderedQuerySources.Scenario);
        _specification = Result.Model.Slices.SelectMany(slice => slice.Specifications).Single();
        _query = new ScreenplayCompiler().Compile(Result.Source).Value!.Modules
            .SelectMany(module => module.Features)
            .SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.Specifications)
            .Single()
            .ThenQueries
            .Single();
    }

    [Fact] void should_generate_a_document_that_binds() => AssertDocument();
    [Fact] void should_not_leave_the_scenario_out() => Result.Diagnostics.Where(diagnostic => string.Equals(diagnostic.Code, ScreenplayDiagnosticCodes.UnreadableSpecification, StringComparison.Ordinal) || string.Equals(diagnostic.Code, ScreenplayDiagnosticCodes.ScenarioWithoutCounterpart, StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_recover_the_query_the_scenario_calls() => _specification.Read!.Query.ShouldEqual("ProjectById");
    [Fact] void should_recover_no_command_to_execute() => _specification.When.ShouldBeNull();
    [Fact] void should_recover_no_given_state() => _specification.Given.ShouldBeEmpty();
    [Fact] void should_state_the_query_by_its_declared_name() => _query.Query.ShouldEqual("ProjectById");
    [Fact] void should_state_the_key_as_the_only_argument() => _query.Arguments.Select(argument => argument.Property).ShouldEqual(["projectId"]);
    [Fact] void should_state_one_expected_read_model() => _query.Results.Count().ShouldEqual(1);
    [Fact] void should_state_every_read_model_value_in_declaration_order() => _query.Results.Single().Properties.Select(property => property.Property).ShouldEqual(["projectId", "name", "number", "startedOn", "updatedAt"]);
    [Fact] void should_state_the_expected_read_model_as_a_subset() => _query.Results.Single().Exactly.ShouldBeFalse();
}
