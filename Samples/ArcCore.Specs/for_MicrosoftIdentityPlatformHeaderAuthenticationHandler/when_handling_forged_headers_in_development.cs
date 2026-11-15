// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.ArcCore.Specs.for_MicrosoftIdentityPlatformHeaderAuthenticationHandler;

public class when_handling_forged_headers_in_development : given.a_handler_in_environment
{
    async Task Because() => _result = await Handle("Development");

    [Fact] void should_authenticate() => _result.IsAuthenticated.ShouldBeTrue();
}
