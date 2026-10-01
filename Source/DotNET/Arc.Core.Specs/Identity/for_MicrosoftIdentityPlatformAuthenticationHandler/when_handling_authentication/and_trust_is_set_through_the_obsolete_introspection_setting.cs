// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_authentication;

public class and_trust_is_set_through_the_obsolete_introspection_setting : given.a_handler
{
    AuthenticationResult _result;

    async Task Because()
    {
#pragma warning disable CS0618 // Type or member is obsolete - the alias is what this spec pins
        var options = new ArcOptions { Introspection = new() { TrustForwardedIdentityHeaders = true } };
#pragma warning restore CS0618
        var handler = new MicrosoftIdentityPlatformAuthenticationHandler(Options.Create(options), NullLoggerFactory.Instance);
        _result = await handler.HandleAuthentication(ContextForPrincipal("workforce"));
    }

    [Fact] void should_use_the_trusted_ingress_identity() => _result.IsAuthenticated.ShouldBeTrue();
}
