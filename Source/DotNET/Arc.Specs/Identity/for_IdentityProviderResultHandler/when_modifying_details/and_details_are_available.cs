// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_modifying_details;

public class and_details_are_available : given.an_identity_provider_result_handler
{
    TestDetails _originalDetails;
    TestDetails _modifiedDetails;

    void Establish()
    {
        _originalDetails = new TestDetails("Engineering", "Developer");
        _modifiedDetails = new TestDetails("Marketing", "Manager");

        var originalResult = new IdentityProviderResult(
            new IdentityId("user123"),
            new IdentityName("Test User"),
            true,
            true,
            _originalDetails);

        _identityProvider.Provide(Arg.Any<IdentityProviderContext>())
            .Returns(Task.FromResult(new IdentityDetails(true, _originalDetails)));

        _httpRequestContext.User.Returns(CreateAuthenticatedUser());
        _httpRequestContext.IsHttps.Returns(true);
    }

    async Task Because() => await _handler.ModifyDetails<TestDetails>(details => _modifiedDetails);

    [Fact] void should_call_write_with_modified_details() =>
        _httpRequestContext.Received(1).WriteAsync(Arg.Is<string>(json => json.Contains("Marketing") && json.Contains("Manager")));

    [Fact] void should_append_cookie() =>
        _httpRequestContext.Received(1).AppendCookie(IdentityProviderResultHandler.IdentityCookieName, Arg.Any<string>(), Arg.Any<CookieOptions>());

    public record TestDetails(string Department, string Role);
}
