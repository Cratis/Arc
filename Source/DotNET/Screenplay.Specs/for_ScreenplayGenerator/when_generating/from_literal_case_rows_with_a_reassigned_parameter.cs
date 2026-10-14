// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_literal_case_rows_with_a_reassigned_parameter : a_generated_document
{
    void Because() => Generate(LiteralCaseSources.With(LiteralCaseSources.Scenario.Replace(
        "void Establish() => _scenario.Given.ForEventSource(\"author\").ReadModel(new Notes(\"author\", note));",
        "void Establish() => name = \"changed\";",
        StringComparison.Ordinal)));

    [Fact] void should_not_substitute_constructor_constants_into_mutated_parameters() => Result.Source.ShouldNotContain("specification ");
    [Fact] void should_keep_the_ordinary_unreadable_row_diagnostics() => Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldEqual(3);
    [Fact] void should_explain_the_failed_parameter_usage_proof() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.SpecificationCasesNotFolded).ShouldContain(diagnostic => diagnostic.Message.Contains("outside a supported whole value position", StringComparison.Ordinal));
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
