// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_StateAwareAuthorizationPolicy;

public class when_the_store_is_unavailable : given.a_plan_policy
{
    void Establish() => _policy.ReadFailure = new InvalidOperationException("store down");

    async Task Because() => await Authorize();

    [Fact] void should_deny() => _allowed.ShouldBeFalse();
    [Fact] void should_report_the_failed_read_not_throw() => _policy.Denials.ShouldEqual([StateDenialReason.ReadFailed]);
}
