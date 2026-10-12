// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_command_scenario_with_a_recovered_caller : a_generated_document
{
    [Theory]
    [InlineData(CallerScenarioSources.RolePrincipal, "[Authorize(Roles = \"Librarian\")]", "role \"Librarian\"")]
    [InlineData("new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(\"tenant\", \"a\"), new Claim(\"tenant\", \"b\"), new Claim(\"tenant\", \"a\") }, \"test\"))", "[Authorize]", "claim \"tenant\" = \"a\"")]
    [InlineData("new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim(ClaimTypes.Role, \"Librarian\") }, \"test\"))", "[Authorize]", "role \"Librarian\"")]
    [InlineData("new ClaimsPrincipal()", "", "given caller")]
    [InlineData("new ClaimsPrincipal(new ClaimsIdentity())", "", "given caller")]
    public void should_state_the_exact_caller_and_bind(string principal, string authorization, string expected)
    {
        Generate(CallerScenarioSources.With(CallerScenarioSources.Setup(principal), authorization));
        Result.Source.ShouldContain($"specification {CallerScenarioSources.ScenarioName}");
        Result.Source.ShouldContain("given caller");
        Result.Source.ShouldContain(expected);
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == "SP0075" || diagnostic.Code == "SP0039").ShouldBeFalse();
        AssertDocument();
    }

    [Theory]
    [InlineData("_accessor = Substitute.For<ICurrentPrincipalAccessor>();", "ICurrentPrincipalAccessor _accessor = null!;")]
    [InlineData("", "readonly ICurrentPrincipalAccessor _accessor = Substitute.For<ICurrentPrincipalAccessor>();")]
    public void should_read_a_stable_field_and_an_explicitly_typed_instance_registration(string assignment, string field)
    {
        var setup = $"{assignment} _accessor.Current.Returns({CallerScenarioSources.RolePrincipal}); _scenario.Services.AddSingleton<ICurrentPrincipalAccessor>(_accessor);";
        var sources = CallerScenarioSources.With(setup);
        sources = sources.Select(source => (source.Path, source.Text.Replace("readonly CommandScenario<RegisterAuthor> _scenario", field + "\n    readonly CommandScenario<RegisterAuthor> _scenario", StringComparison.Ordinal))).ToArray();
        Generate(sources);
        Result.Source.ShouldContain($"specification {CallerScenarioSources.ScenarioName}");
        Result.Source.ShouldContain("role \"Librarian\"");
        AssertDocument();
    }

    [Fact]
    public void should_preserve_claim_repetition_order_casing_and_escaped_literals()
    {
        const string Principal = "new ClaimsPrincipal(new ClaimsIdentity([new Claim(\"Tenant\", \"a\\\"b\"), new Claim(\"tenant\", \"second\"), new Claim(\"Tenant\", \"a\\\"b\"), new Claim(ClaimTypes.Role, \"Librarian\"), new Claim(ClaimTypes.Role, \"Librarian\")], \"test\"))";
        Generate(CallerScenarioSources.With(CallerScenarioSources.Setup(Principal)));
        var claims = Result.Source.Split('\n').Where(line => line.TrimStart().StartsWith("claim ", StringComparison.Ordinal)).Select(line => line.Trim()).ToArray();
        claims.Length.ShouldEqual(3);
        claims[0].ShouldEqual(claims[2]);
        claims[1].ShouldEqual("claim \"tenant\" = \"second\"");
        Result.Source.Split('\n').Count(line => line.Trim() == "role \"Librarian\"").ShouldEqual(2);
        AssertDocument();
    }
}
