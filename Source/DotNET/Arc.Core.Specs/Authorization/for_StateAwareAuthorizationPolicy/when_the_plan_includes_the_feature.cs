// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_StateAwareAuthorizationPolicy;

public class when_the_plan_includes_the_feature : given.a_plan_policy
{
    void Establish() => _policy.Plans["vessel-1"] = new(true);

    async Task Because() => await Authorize();

    [Fact] void should_allow() => _allowed.ShouldBeTrue();
}
