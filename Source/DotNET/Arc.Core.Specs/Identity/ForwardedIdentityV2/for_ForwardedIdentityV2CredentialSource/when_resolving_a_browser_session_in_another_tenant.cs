// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.ForwardedIdentityV2.given;

namespace Cratis.Arc.Identity.ForwardedIdentityV2.for_ForwardedIdentityV2CredentialSource;

public class when_resolving_a_browser_session_in_another_tenant : given.a_browser_session_resolution
{
    CallerResolution _result;
    async Task Because() => _result = await Resolve(VerificationFixture.DirectBrowser(), new("other-tenant"), new());
    [Fact] void should_refuse() => _result.Refusal.ShouldEqual(CallerRefusal.TenantMismatch);
    [Fact] void should_have_no_caller() => _result.Caller.ShouldBeNull();
}
