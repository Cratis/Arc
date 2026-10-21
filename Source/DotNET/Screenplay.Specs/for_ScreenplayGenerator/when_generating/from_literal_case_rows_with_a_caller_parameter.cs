// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_literal_case_rows_with_a_caller_parameter : a_generated_document
{
    void Because()
    {
        var sources = CallerScenarioSources.With(
            CallerScenarioSources.Setup("new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], \"test\"))"),
            authorization: "");
        Generate(sources.Select(source => (source.Path, source.Text.Replace(
            "public class with_a_caller",
            "public abstract class with_a_caller(string role)",
            StringComparison.Ordinal) +
            (source.Path.EndsWith("with_a_caller.cs", StringComparison.Ordinal)
                ? "\npublic class Librarian() : with_a_caller(\"Librarian\");\npublic class Editor() : with_a_caller(\"Editor\");"
                : string.Empty))).ToArray());
    }

    [Fact] void should_never_put_a_case_reference_in_a_caller() => Result.Source.ShouldNotContain("case.role");
    [Fact] void should_explain_the_excluded_position() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.SpecificationCasesNotFolded).ShouldContain(diagnostic => diagnostic.Message.Contains("callers", StringComparison.Ordinal));
    [Fact] void should_keep_the_ordinary_unauthorized_rows() => Result.Source.Split('\n').Count(line => line.TrimStart().StartsWith("specification ", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_round_trip_and_bind() => AssertDocument();
}
