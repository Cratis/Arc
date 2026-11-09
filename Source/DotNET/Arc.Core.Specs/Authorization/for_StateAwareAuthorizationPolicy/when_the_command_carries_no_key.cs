// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_StateAwareAuthorizationPolicy;

public class when_the_command_carries_no_key : given.a_plan_policy
{
    void Establish() => _context = new AuthorizationPolicyContext(_context.Principal, _context.Target, new object());

    async Task Because() => await Authorize();

    [Fact] void should_deny() => _allowed.ShouldBeFalse();
    [Fact] void should_report_the_missing_key() => _policy.Denials.ShouldEqual([StateDenialReason.NoKey]);
}
