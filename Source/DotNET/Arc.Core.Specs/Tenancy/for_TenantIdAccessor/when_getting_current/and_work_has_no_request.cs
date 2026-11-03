// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_getting_current;

public class and_work_has_no_request : given.a_verified_tenant_id_accessor
{
    TenantId _result;
    void Establish() => _requests.Current.Returns((IHttpRequestContext)null);
    void Because() => _result = _accessor.Current;
    [Fact] void should_preserve_resolution() => _result.ShouldEqual(new TenantId("selected"));
    [Fact] void should_not_call_membership() => _membership.DidNotReceive().IsMember(Arg.Any<ClaimsPrincipal>(), Arg.Any<TenantId>());
}
