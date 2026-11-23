// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_comparing_with_the_pipeline : given.an_evaluation
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    async Task should_match_role_authorization(bool authenticated)
    {
        var principal = authenticated ? Principal : new ClaimsPrincipal();
        await using var scenario = new CommandScenario<given.RoleCommand>();
        foreach (var descriptor in Services) scenario.Services.Add(descriptor);
        scenario.WithPrincipal(principal);
        var standalone = await CommandAuthorization.For<given.RoleCommand>(Provider, principal, TestContext.Current.CancellationToken);
        var pipeline = await scenario.Execute(new given.RoleCommand(), TestContext.Current.CancellationToken);
        (standalone.Outcome == CommandAuthorizationOutcome.Authorized).ShouldEqual(pipeline.IsAuthorized);
        Behavior.HandlerCalls.ShouldEqual(authenticated ? 1 : 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    async Task should_match_resource_policy_authorization(bool allow)
    {
        await using var scenario = new CommandScenario<given.ResourceCommand>();
        foreach (var descriptor in Services) scenario.Services.Add(descriptor);
        scenario.WithPrincipal(Principal);
        var command = new given.ResourceCommand(allow);
        var standalone = await CommandAuthorization.For(Provider, Principal, command, TestContext.Current.CancellationToken);
        var pipeline = await scenario.Execute(command, TestContext.Current.CancellationToken);
        (standalone.Outcome == CommandAuthorizationOutcome.Authorized).ShouldEqual(pipeline.IsAuthorized);
        Behavior.HandlerCalls.ShouldEqual(allow ? 1 : 0);
    }
}
