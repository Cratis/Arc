// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Testing.Commands;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_fallback_requirements_apply : given.an_evaluation
{
    void Establish()
    {
        var fallback = Substitute.For<IFallbackAuthorizationEvaluator>();
        fallback.GetAuthorizationRequirements(Arg.Any<Type>()).Returns([new AuthorizationRequirement(["Admin"])]);
        Services.AddSingleton<IInstancesOf<IFallbackAuthorizationEvaluator>>(new KnownInstancesOf<IFallbackAuthorizationEvaluator>([fallback]));
    }

    async Task Because() => Result = await CommandAuthorization.For<given.UnrestrictedCommand>(Provider, Principal);
    [Fact] void should_deny() => Result.ShouldBeDenied();

    [Fact] async Task should_match_the_pipeline()
    {
        await using var scenario = new CommandScenario<given.UnrestrictedCommand>();
        foreach (var descriptor in Services) scenario.Services.Add(descriptor);
        scenario.WithPrincipal(Principal);
        var pipeline = await scenario.Execute(new given.UnrestrictedCommand(), TestContext.Current.CancellationToken);
        pipeline.IsAuthorized.ShouldBeFalse();
    }

    [Fact] async Task should_allow_a_matching_fallback_role()
    {
        var admin = Cratis.Arc.Authorization.SystemPrincipal.WithRoles("Admin");
        (await CommandAuthorization.For<given.UnrestrictedCommand>(Provider, admin, TestContext.Current.CancellationToken)).ShouldBeAuthorized();
    }
    [Fact] async Task should_not_override_an_explicit_declaration() => (await CommandAuthorization.For<given.RoleCommand>(Provider, Principal, TestContext.Current.CancellationToken)).ShouldBeAuthorized();
    [Fact] async Task should_not_override_anonymous_access() => (await CommandAuthorization.For<given.AnonymousCommand>(Provider, Principal, TestContext.Current.CancellationToken)).ShouldBeAuthorized();
}
