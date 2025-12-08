// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_modifying_details;

public class and_details_type_does_not_match : given.an_identity_provider_result_handler
{
    void Establish()
    {
        const string wrongTypeDetails = "This is a string, not the expected type";

        _identityProvider.Provide(Arg.Any<IdentityProviderContext>())
            .Returns(Task.FromResult(new IdentityDetails(true, wrongTypeDetails)));

        _httpRequestContext.User.Returns(CreateAuthenticatedUser());
    }

    async Task Because() => await _handler.ModifyDetails<int>(details => 42);

    [Fact] void should_not_call_write() => _httpRequestContext.DidNotReceive().WriteAsync(Arg.Any<string>());
    [Fact] void should_not_append_cookie() => _httpRequestContext.DidNotReceive().AppendCookie(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CookieOptions>());
}
