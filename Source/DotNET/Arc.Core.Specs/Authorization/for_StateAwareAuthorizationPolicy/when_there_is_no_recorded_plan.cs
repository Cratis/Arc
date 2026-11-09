// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_StateAwareAuthorizationPolicy;

public class when_there_is_no_recorded_plan : given.a_plan_policy
{
    void Establish() => _policy.Plans.Clear();

    async Task Because() => await Authorize();

    [Fact] void should_deny() => _allowed.ShouldBeFalse();
    [Fact] void should_report_the_absence() => _policy.Denials.ShouldEqual([StateDenialReason.NotFound]);
}
