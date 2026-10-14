// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_denial_scenario_with_an_allowed_caller : a_generated_document
{
    [Theory]
    [InlineData("[Authorize]", "new ClaimsPrincipal(new ClaimsIdentity([], \"test\"))", "")]
    [InlineData("[Authorize(Roles = \"Librarian\")]", CallerScenarioSources.RolePrincipal, "")]
    [InlineData("[Authorize(Roles = \"Librarian,Administrator\")]", CallerScenarioSources.RolePrincipal, "")]
    [InlineData("[Authorize(Policy = \"Tenant\")]", "new ClaimsPrincipal(new ClaimsIdentity([new Claim(\"TENANT\", \"a\")], \"test\"))", "public static class Composition { public static void Configure(Microsoft.AspNetCore.Authorization.AuthorizationOptions options) => options.AddPolicy(\"Tenant\", p => p.RequireAuthenticatedUser().RequireClaim(\"tenant\", \"a\")); }")]
    public void should_omit_a_caller_that_satisfies_the_emitted_gate(string authorization, string principal, string composition)
    {
        Generate(DenialScenarioSources.With(CallerScenarioSources.Setup(principal), authorization, composition));
        Result.Source.ShouldNotContain("then denied");
        Result.Source.ShouldNotContain($"specification {CallerScenarioSources.ScenarioName}");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("stated caller satisfies the stated authorization");
        AssertDocument();
    }

    [Fact]
    public void should_omit_a_policy_whose_caller_outcome_is_undecidable()
    {
        const string Composition = """
            public static class Composition
            {
                public static void Configure(Microsoft.AspNetCore.Authorization.AuthorizationOptions options) => options.AddPolicy("Tenant", p => p.RequireAssertion(context => true));
            }
            """;
        Generate(DenialScenarioSources.With(CallerScenarioSources.Setup("new ClaimsPrincipal()"), "[Authorize(Policy = \"Tenant\")]", Composition));
        Result.Source.ShouldNotContain("then denied");
        Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).Message.ShouldContain("denial cannot be decided from the stated caller");
        AssertDocument();
    }
}
