// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.when_handling_authentication;

public class and_the_original_constructor_resolves_the_host_opt_in : given.a_legacy_handler
{
    AuthenticateResult _result;

    async Task Because() => _result = await AuthenticateLegacy(true);

    [Fact] void should_authenticate_forwarded_headers() => _result.Succeeded.ShouldBeTrue();
    [Fact] void should_preserve_the_forwarded_identity() => _result.Principal!.Identity!.Name.ShouldEqual(UserDetails);
}
