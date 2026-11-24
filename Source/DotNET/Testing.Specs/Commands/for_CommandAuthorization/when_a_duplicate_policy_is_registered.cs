// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_a_duplicate_policy_is_registered : given.an_evaluation
{
    void Establish() => Services.AddArcAuthorizationPolicy<given.FixturePolicy>("fixture", evaluatesAnonymous: true);
    async Task Because() => Result = await CommandAuthorization.For(Provider, Principal, new given.ResourceCommand(true));
    [Fact] void should_be_unsupported() => Result.ShouldBeUnsupported();
    [Fact] void should_explain_the_configuration() => Result.ReasonCode.ShouldEqual("invalid-configuration");
}
