// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_mutating_a_consumed_input : given.a_signed_request
{
    ForwardedIdentityAuthenticationResult _result;
    ForwardedIdentityAuthenticationResult _restored;

    void Because()
    {
        Authenticate().Principal.ShouldNotBeNull();
        _rawHeaders[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = ["attacker"];
        _result = Authenticate();
        _rawHeaders[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = ["n"];
        _restored = Authenticate();
    }

    [Fact] void should_not_return_the_cached_principal() => _result.Principal.ShouldBeNull();
    [Fact] void should_not_restore_a_stale_success_after_failure() => _restored.Principal.ShouldBeNull();
}
