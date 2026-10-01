// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIDentityPlatformAuthHandler.when_handling_authentication;

public class and_the_original_constructor_has_no_arc_options : given.a_legacy_handler
{
    AuthenticateResult _result;

    async Task Because() => _result = await AuthenticateLegacy(null);

    [Fact] void should_not_authenticate_forwarded_headers() => _result.None.ShouldBeTrue();
}
