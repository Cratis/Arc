// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_the_legacy_evaluator_denies : given.an_evaluation
{
    void Establish()
    {
        var evaluator = Substitute.For<IAuthorizationEvaluator>();
        evaluator.IsAuthorized(Arg.Any<Type>()).Returns(false);
        Services.AddSingleton(evaluator);
    }

    async Task Because() => Result = await CommandAuthorization.For<given.RoleCommand>(Provider, Principal);
    [Fact] void should_preserve_the_legacy_denial() => Result.ShouldBeDenied();
}
