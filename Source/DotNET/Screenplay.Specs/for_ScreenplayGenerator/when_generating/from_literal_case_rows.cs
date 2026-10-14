// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_literal_case_rows : a_generated_document
{
    void Because() => Generate(LiteralCaseSources.With(LiteralCaseSources.Scenario));

    [Fact] void should_state_one_table() => Result.Source.Split('\n').Count(line => line.TrimStart().StartsWith("specification ", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_name_the_shared_scenario() => Result.Source.ShouldContain("specification Registering");
    [Fact] void should_keep_the_concept_target_type() => Result.Source.ShouldContain("parameter name AuthorName");
    [Fact] void should_keep_the_optional_target_type() => Result.Source.ShouldContain("parameter note String optional");
    [Fact] void should_use_the_parameter_in_the_steps() => Result.Source.ShouldContain("name = case.name");
    [Fact] void should_state_all_three_named_rows() => Result.Source.Split('\n').Count(line => line.TrimStart().StartsWith("case ", StringComparison.Ordinal)).ShouldEqual(3);
    [Fact] void should_state_the_optional_null() => Result.Source.ShouldContain("note = null");
    [Fact] void should_bind_all_expanded_names() => SemanticExecutionPlan.Compile(Bound.Value!.Model).Plan!.Specifications.Values.Select(specification => specification.Name).ShouldContainOnly(["Registering_Jane", "Registering_John", "Registering_Empty"]);
    [Fact] void should_not_report_unfolded_cases() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.SpecificationCasesNotFolded).ShouldBeEmpty();
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
