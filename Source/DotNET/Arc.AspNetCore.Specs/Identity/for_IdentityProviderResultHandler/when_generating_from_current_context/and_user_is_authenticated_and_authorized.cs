// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_generating_from_current_context;

public class and_user_is_authenticated_and_authorized : given.an_identity_provider_result_handler
{
    IdentityProviderResult _result;
    IdentityDetails _identityDetails;
    object _expectedDetails;

    void Establish()
    {
        var claims = new[]
        {
            new Claim("sub", "user123"),
            new Claim("name", "Test User"),
            new Claim(ClaimTypes.Name, "Test User")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        _httpContext.User = new ClaimsPrincipal(identity);

        _expectedDetails = new { Department = "Engineering", Role = "Developer" };
        _identityDetails = new IdentityDetails(true, _expectedDetails);
        _identityProvider.Provide(Arg.Any<IdentityProviderContext>()).Returns(_identityDetails);
    }

    async Task Because() => _result = await _handler.GenerateFromCurrentContext();

    [Fact] void should_return_identity_provider_result_with_correct_id() => _result.Id.ShouldEqual(new IdentityId("user123"));
    [Fact] void should_return_identity_provider_result_with_correct_name() => _result.Name.ShouldEqual(new IdentityName("Test User"));
    [Fact] void should_return_identity_provider_result_that_is_authenticated() => _result.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_return_identity_provider_result_that_is_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_return_identity_provider_result_with_correct_details() => _result.Details.ShouldEqual(_expectedDetails);
    [Fact] void should_call_identity_provider_with_correct_context() => _identityProvider.Received(1).Provide(Arg.Is<IdentityProviderContext>(ctx =>
        ctx.Id == "user123" &&
        ctx.Name == "Test User" &&
        ctx.Claims.Any(c => c.Key == "sub" && c.Value == "user123")));
}