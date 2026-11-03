// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_reentering_before_the_inner_assertion_expires : given.a_short_lived_actor_forward
{
    ForwardedIdentityAuthenticationResult _again;

    void Because()
    {
        Authenticate();
        _clock.GetUtcNow().Returns(_now.AddSeconds(34));
        _again = Authenticate();
    }

    [Fact] void should_reuse_the_combined_result() => _again.Principal.ShouldNotBeNull();
}
