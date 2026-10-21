// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_denial_scenario : a_generated_document
{
    [Theory]
    [InlineData("_result.ShouldNotBeAuthorized();")]
    [InlineData("_result.ShouldNotBeAuthorized(); _result.ShouldNotBeSuccessful();")]
    [InlineData("_result.ShouldNotBeSuccessful(); _result.ShouldNotBeAuthorized();")]
    public void should_bind_and_execute_an_exact_authentication_denial(string assertions)
    {
        Generate(DenialScenarioSources.With(CallerScenarioSources.Setup("new ClaimsPrincipal()"), assertions: assertions));
        AssertDenial();
    }

    [Theory]
    [InlineData("[Authorize(Roles = \"Librarian\")]", "")]
    [InlineData("[Authorize(Policy = \"Tenant\")]", "public static class Composition { public static void Configure(Microsoft.AspNetCore.Authorization.AuthorizationOptions options) => options.AddPolicy(\"Tenant\", p => p.RequireAuthenticatedUser().RequireClaim(\"tenant\", \"a\")); }")]
    public void should_bind_and_execute_an_exact_policy_denial(string authorization, string composition)
    {
        Generate(DenialScenarioSources.With(
            CallerScenarioSources.Setup("new ClaimsPrincipal(new ClaimsIdentity([], \"test\"))"), authorization, composition));
        AssertDenial();
    }

    void AssertDenial()
    {
        Result.Source.ShouldContain("then denied");
        Result.Source.ShouldNotContain("then error");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnreadableSpecification).ShouldBeEmpty();
        AssertDocument();
        var run = Run(CallerScenarioSources.ScenarioName);
        Assert.True(run.Passed, string.Join(Environment.NewLine, run.Failures) + Environment.NewLine + Result.Source);
    }
}
