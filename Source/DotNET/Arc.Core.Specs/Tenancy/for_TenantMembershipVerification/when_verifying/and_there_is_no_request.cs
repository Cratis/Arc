// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_there_is_no_request : given.a_tenant_membership_verification
{
    bool _result;
    void Establish() => _requests.Current.Returns((IHttpRequestContext)null);
    void Because() => _result = _verification.Verify(_tenant);
    [Fact] void should_not_check_membership() => _result.ShouldBeFalse();
    [Fact] void should_not_call_the_hook() => _membership.DidNotReceive().IsMember(Arg.Any<ClaimsPrincipal>(), Arg.Any<TenantId>());
}
