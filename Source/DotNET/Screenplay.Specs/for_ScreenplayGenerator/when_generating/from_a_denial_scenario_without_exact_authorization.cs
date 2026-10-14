// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_denial_scenario_without_exact_authorization : a_generated_document
{
    [Theory]
    [InlineData("p.RequireAuthenticatedUser().RequireAssertion(context => true)")]
    [InlineData("p.RequireAssertion(context => true)")]
    [InlineData("")]
    public void should_omit_a_partial_or_synthesized_policy(string policy)
    {
        var composition = policy.Length == 0 ? "" : $$"""
            public static class Composition
            {
                public static void Configure(Microsoft.AspNetCore.Authorization.AuthorizationOptions options) => options.AddPolicy("Tenant", p => {{policy}});
            }
            """;
        Generate(DenialScenarioSources.With(CallerScenarioSources.Setup("new ClaimsPrincipal()"), "[Authorize(Policy = \"Tenant\")]", composition));
        AssertOmitted("policy");
    }

    [Fact]
    public void should_omit_an_absent_caller()
    {
        Generate(DenialScenarioSources.With(""));
        AssertOmitted("caller");
    }

    [Fact]
    public void should_omit_an_inexact_caller()
    {
        Generate(DenialScenarioSources.With(CallerScenarioSources.Setup("new ClaimsPrincipal(new ClaimsIdentity([new Claim(\"tenant\", Computed())], \"test\"))")));
        AssertOmitted("caller");
    }

    [Fact]
    public void should_omit_a_denial_without_a_stated_gate()
    {
        Generate(DenialScenarioSources.With(CallerScenarioSources.Setup("new ClaimsPrincipal()"), ""));
        AssertOmitted("denial not explained by any stated policy");
    }

    void AssertOmitted(string reason)
    {
        Result.Source.ShouldNotContain("then denied");
        Result.Source.ShouldNotContain($"specification {CallerScenarioSources.ScenarioName}");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain(reason);
        AssertDocument();
    }
}
