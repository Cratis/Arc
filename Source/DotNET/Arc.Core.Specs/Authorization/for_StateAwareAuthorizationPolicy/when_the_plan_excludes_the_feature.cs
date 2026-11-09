// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_StateAwareAuthorizationPolicy;

public class when_the_plan_excludes_the_feature : given.a_plan_policy
{
    void Establish() => _policy.Plans["vessel-1"] = new(false);

    async Task Because() => await Authorize();

    [Fact] void should_deny() => _allowed.ShouldBeFalse();
    [Fact] void should_report_the_rejection() => _policy.Denials.ShouldEqual([StateDenialReason.Rejected]);
}
