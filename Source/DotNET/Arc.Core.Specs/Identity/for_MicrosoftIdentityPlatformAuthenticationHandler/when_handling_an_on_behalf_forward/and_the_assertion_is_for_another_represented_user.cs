// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;
using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_an_on_behalf_forward;

public class and_the_assertion_is_for_another_represented_user : given.an_on_behalf_forward
{
    AuthenticationResult _anotherPrincipalHeader;
    AuthenticationResult _anotherSubject;
    AuthenticationResult _anotherPrincipalId;

    async Task Because()
    {
        _anotherPrincipalHeader = await _handler.HandleAuthentication(ContextForOnBehalfOf(ActorHeaderValue(), claims => claims["principal_hash"] = OnBehalfOfAssertionsForSpecs.Sha256("another principal")));
        _anotherSubject = await _handler.HandleAuthentication(ContextForOnBehalfOf(ActorHeaderValue(), claims => claims["sub"] = "another-user"));

        var context = ContextForOnBehalfOf(ActorHeaderValue());
        var headers = new Dictionary<string, string>(context.Headers) { [MicrosoftIdentityPlatformHeaders.IdentityIdHeader] = "another-user" };
        context.Headers.Returns(headers);
        _anotherPrincipalId = await _handler.HandleAuthentication(context);
    }

    [Fact] void should_reject_a_principal_header_the_assertion_does_not_cover() => _anotherPrincipalHeader.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.PrincipalMismatch);
    [Fact] void should_reject_a_subject_that_is_not_the_represented_user() => _anotherSubject.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.PrincipalMismatch);
    [Fact] void should_reject_a_principal_id_header_that_is_not_the_represented_user() => _anotherPrincipalId.Failure!.Reason.Value.ShouldEqual(OnBehalfOfFailures.PrincipalMismatch);
}
