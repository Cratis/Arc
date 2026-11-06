// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Capabilities.for_VerifiedCallerAuthenticationHandler;

public class when_no_caller_was_handed_over : given.an_authentication_context
{
    AuthenticateResult _absent;
    AuthenticateResult _wrongType;

    async Task Because()
    {
        _absent = await _context.AuthenticateAsync(VerifiedCallerAuthentication.Scheme);
        _context.Items[VerifiedCallerPrincipal.CallerItemKey] = "not a caller";
        _wrongType = await _context.AuthenticateAsync(VerifiedCallerAuthentication.Scheme);
    }

    [Fact] void should_not_authenticate_when_absent() => _absent.Succeeded.ShouldBeFalse();

    [Fact] void should_not_authenticate_for_a_foreign_item() => _wrongType.Succeeded.ShouldBeFalse();

    [Fact] void should_not_fail_either() => _absent.Failure.ShouldBeNull();
}
