// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;
using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_an_on_behalf_forward;

public class and_the_represented_user_is_missing : given.an_on_behalf_forward
{
    AuthenticationResult _result;

    async Task Because()
    {
        var context = Substitute.For<IHttpRequestContext>();
        context.Headers.Returns(new Dictionary<string, string> { [OnBehalfOfHeaders.ActorHeader] = ActorHeaderValue() });
        _result = await _handler.HandleAuthentication(context);
    }

    [Fact] void should_not_authenticate_the_request() => _result.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_reject_the_forward_instead_of_treating_it_as_anonymous() => _result.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.MissingRepresentedUser);
}
