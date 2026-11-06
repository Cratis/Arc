// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Capabilities.for_VerifiedCallerAuthenticationHandler;

public class when_the_gate_handed_over_a_caller : given.an_authentication_context
{
    VerifiedCaller _caller;
    AuthenticateResult _result;

    void Establish()
    {
        _caller = new("1", CallerPrincipalKind.Person, CallerCredentialKind.BrowserSession, new("credential"), null, new("person"), null, null, null, null, null, "resource", null, null, null, "test", new HashSet<string>(), new HashSet<string>(), null);
        _context.Items[VerifiedCallerPrincipal.CallerItemKey] = _caller;
    }

    async Task Because() => _result = await _context.AuthenticateAsync(VerifiedCallerAuthentication.Scheme);

    [Fact] void should_succeed() => _result.Succeeded.ShouldBeTrue();

    [Fact] void should_present_the_verified_caller() => VerifiedCallerPrincipal.Carries(_result.Principal, _caller).ShouldBeTrue();
}
