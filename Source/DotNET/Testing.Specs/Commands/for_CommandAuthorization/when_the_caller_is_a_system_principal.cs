// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_the_caller_is_a_system_principal : given.an_evaluation
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    async Task should_match_the_pipeline_without_bypassing_roles(bool writer)
    {
        var principal = writer ? SystemPrincipal.WithRoles("Writer") : SystemPrincipal.WithRoles();
        await using var scenario = new CommandScenario<given.RoleCommand>();
        foreach (var descriptor in Services) scenario.Services.Add(descriptor);
        scenario.WithPrincipal(principal);
        var standalone = await CommandAuthorization.For<given.RoleCommand>(Provider, principal, TestContext.Current.CancellationToken);
        var pipeline = await scenario.Execute(new given.RoleCommand(), TestContext.Current.CancellationToken);
        (standalone.Outcome == CommandAuthorizationOutcome.Authorized).ShouldEqual(writer);
        pipeline.IsAuthorized.ShouldEqual(writer);
    }
}
