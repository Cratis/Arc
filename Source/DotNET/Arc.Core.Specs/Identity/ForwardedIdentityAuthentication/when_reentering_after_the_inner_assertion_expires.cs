// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_reentering_after_the_inner_assertion_expires : given.a_short_lived_actor_forward
{
    ForwardedIdentityAuthenticationResult _first;
    ForwardedIdentityAuthenticationResult _again;

    void Because()
    {
        _first = Authenticate();
        _clock.GetUtcNow().Returns(_now.AddSeconds(35));
        _again = Authenticate();
    }

    [Fact] void should_authenticate_the_first_time() => _first.Principal.ShouldNotBeNull();
    [Fact] void should_not_reuse_the_combined_result() => _again.Principal.ShouldBeNull();
    [Fact] void should_fail_the_reentry() => _again.Failure.ShouldNotBeNull();
}
