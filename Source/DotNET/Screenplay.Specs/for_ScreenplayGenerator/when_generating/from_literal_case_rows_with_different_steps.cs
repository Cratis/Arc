// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_literal_case_rows_with_different_steps : a_generated_document
{
    void Because() => Generate(LiteralCaseSources.With(LiteralCaseSources.Scenario.Replace(
        "public class John() : Registering(\"John\", \"second\");",
        "public class John() : Registering(\"John\", \"second\") { void Establish() => _scenario.Given.ForEventSource(\"author\").Events(new AuthorRegistered(\"before\")); }",
        StringComparison.Ordinal)));

    [Fact] void should_not_fold_any_row() => Result.Source.ShouldNotContain("parameter name");
    [Fact] void should_keep_all_readable_rows() => Result.Source.Split('\n').Count(line => line.TrimStart().StartsWith("specification ", StringComparison.Ordinal)).ShouldEqual(3);
    [Fact] void should_explain_the_nonuniform_steps() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.SpecificationCasesNotFolded).ShouldContain(diagnostic => diagnostic.Message.Contains("body-less", StringComparison.Ordinal));
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
