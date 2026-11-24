// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_a_policy_evaluates_anonymous_callers : given.an_evaluation
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    async Task should_match_the_pipeline_for_the_anonymous_resource(bool allow)
    {
        var anonymous = new ClaimsPrincipal();
        var command = new given.ResourceCommand(allow);
        await using var scenario = new CommandScenario<given.ResourceCommand>();
        foreach (var descriptor in Services) scenario.Services.Add(descriptor);
        scenario.WithPrincipal(anonymous);
        var standalone = await CommandAuthorization.For(Provider, anonymous, command, TestContext.Current.CancellationToken);
        var pipeline = await scenario.Execute(command, TestContext.Current.CancellationToken);
        (standalone.Outcome == CommandAuthorizationOutcome.Authorized).ShouldEqual(allow);
        pipeline.IsAuthorized.ShouldEqual(allow);
    }
}
