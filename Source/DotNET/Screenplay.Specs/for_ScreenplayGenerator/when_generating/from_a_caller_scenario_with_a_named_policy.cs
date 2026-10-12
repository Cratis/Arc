// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_caller_scenario_with_a_named_policy : a_generated_document
{
    [Theory]
    [InlineData("p.RequireAuthenticatedUser().RequireClaim(\"tenant\", \"a\")", true)]
    [InlineData("p.RequireAuthenticatedUser().RequireAssertion(context => true)", false)]
    [InlineData("p.RequireAssertion(context => true)", false)]
    [InlineData("", false)]
    public void should_keep_the_caller_only_when_every_policy_was_recovered_exactly(string policy, bool exact)
    {
        var composition = policy.Length == 0 ? "" : $$"""
            public static class Composition
            {
                public static void Configure(Microsoft.AspNetCore.Authorization.AuthorizationOptions options) => options.AddPolicy("Tenant", p => {{policy}});
            }
            """;
        Generate(CallerScenarioSources.With(
            CallerScenarioSources.Setup("new ClaimsPrincipal(new ClaimsIdentity([new Claim(\"tenant\", \"a\")], \"test\"))"),
            "[Authorize(Policy = \"Tenant\")]",
            composition));
        Result.Source.Contains($"specification {CallerScenarioSources.ScenarioName}", StringComparison.Ordinal).ShouldEqual(exact);
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == "SP0039").ShouldEqual(!exact);
        if (!exact)
        {
            Result.Diagnostics.Single(diagnostic => diagnostic.Code == "SP0039").Message.ShouldContain("policy");
        }
        AssertDocument();
    }
}
