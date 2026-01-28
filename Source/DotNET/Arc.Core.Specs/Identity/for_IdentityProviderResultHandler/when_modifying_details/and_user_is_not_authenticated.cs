// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_modifying_details;

public class and_user_is_not_authenticated : given.an_identity_provider_result_handler
{
    void Establish()
    {
        var identity = Substitute.For<ClaimsIdentity>();
        identity.IsAuthenticated.Returns(false);

        var user = Substitute.For<ClaimsPrincipal>();
        user.Identity.Returns(identity);

        _httpRequestContext.User.Returns(user);
    }

    async Task Because() => await _handler.ModifyDetails<object>(details => new { Modified = true });

    [Fact] void should_not_call_write() => _httpRequestContext.DidNotReceive().Write(Arg.Any<string>());
    [Fact] void should_not_append_cookie() => _httpRequestContext.DidNotReceive().AppendCookie(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Http.CookieOptions>());
}
