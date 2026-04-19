// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Arc.Identity.for_IdentityProvider.when_generating_from_current_context;

public class and_identity_cookie_exists : given.an_identity_provider_result_handler
{
    IdentityProviderResult _result;

    void Establish()
    {
        var cookieResult = new IdentityProviderResult(
            new IdentityId("cookie-user"),
            new IdentityName("Cookie User"),
            IsAuthenticated: true,
            IsAuthorized: true,
            Roles: ["Admin", "Reader"],
            Details: new { Department = "R&D" });

        var cookieJson = JsonSerializer.Serialize(cookieResult, _options.JsonSerializerOptions);
        var encodedCookie = Convert.ToBase64String(Encoding.UTF8.GetBytes(cookieJson));

        _httpRequestContext.Cookies.Returns(new Dictionary<string, string>
        {
            [IdentityProvider.IdentityCookieName] = encodedCookie,
        });

        _httpRequestContext.User.Returns((System.Security.Claims.ClaimsPrincipal?)null);
    }

    async Task Because() => _result = await _handler.Get();

    [Fact] void should_return_result_from_cookie() => _result.Id.Value.ShouldEqual("cookie-user");
    [Fact] void should_be_authenticated() => _result.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_not_call_identity_provider() => _identityProvider.DidNotReceive().Provide(Arg.Any<IdentityProviderContext>());
}
