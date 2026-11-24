// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_a_custom_runtime_is_registered : given.an_evaluation
{
    void Establish() => Services.AddSingleton(Substitute.For<IAuthorizationPolicyRuntime>());
    async Task Because() => Result = await CommandAuthorization.For<given.RoleCommand>(Provider, Principal);
    [Fact] void should_be_unsupported() => Result.ShouldBeUnsupported();
    [Fact] void should_identify_the_runtime() => Result.ReasonCode.ShouldEqual("runtime-unsupported");
}
