// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_caller_scenario_with_an_arc_policy : a_generated_document
{
    [Theory]
    [InlineData("context.Principal.Identity?.IsAuthenticated == true && context.Principal.HasClaim(\"tenant\", \"a\")", true)]
    [InlineData("!context.Principal.HasClaim(\"tenant\", \"a\")", false)]
    public void should_keep_the_caller_only_when_the_arc_policy_was_recovered_exactly(string condition, bool exact)
    {
        var composition = $$"""
            public sealed class TenantPolicy : IAuthorizationPolicy
            {
                public System.Threading.Tasks.ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, System.Threading.CancellationToken cancellationToken)
                    => System.Threading.Tasks.ValueTask.FromResult({{condition}});
            }
            public static class Composition
            {
                public static void Configure(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
                    => Cratis.Arc.Authorization.AuthorizationPolicyServiceCollectionExtensions.AddArcAuthorizationPolicy<TenantPolicy>(services, "Tenant");
            }
            """;
        var sources = CallerScenarioSources.With(
            CallerScenarioSources.Setup("new ClaimsPrincipal(new ClaimsIdentity([new Claim(\"tenant\", \"a\")], \"test\"))"),
            "[Authorize(Policy = \"Tenant\")]",
            composition);
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        Generate(sources);
        Result.Source.Contains($"specification {CallerScenarioSources.ScenarioName}", StringComparison.Ordinal).ShouldEqual(exact);
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == "SP0039").ShouldEqual(!exact);
        if (!exact)
        {
            Result.Diagnostics.Single(diagnostic => diagnostic.Code == "SP0039").Message.ShouldContain("policy");
        }
        AssertDocument();
    }
}
