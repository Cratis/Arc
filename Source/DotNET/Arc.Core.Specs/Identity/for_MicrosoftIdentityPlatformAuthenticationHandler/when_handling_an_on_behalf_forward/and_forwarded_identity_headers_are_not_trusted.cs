// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_an_on_behalf_forward;

public class and_forwarded_identity_headers_are_not_trusted : given.an_on_behalf_forward
{
    AuthenticationResult _result;

    async Task Because()
    {
        var handler = new MicrosoftIdentityPlatformAuthenticationHandler(Options.Create(new ArcOptions()), NullLoggerFactory.Instance);
        _result = await handler.HandleAuthentication(ContextForOnBehalfOf(ActorHeaderValue()));
    }

    [Fact] void should_not_authenticate_the_request() => _result.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_reject_the_request_instead_of_treating_it_as_anonymous() => _result.Failure.ShouldNotBeNull();
    [Fact] void should_say_the_forward_is_not_trusted() => _result.Failure!.Reason.Value.ShouldEqual(ForwardedActor.UntrustedForward);
}
