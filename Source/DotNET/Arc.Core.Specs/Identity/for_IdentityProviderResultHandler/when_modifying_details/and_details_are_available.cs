// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_IdentityProvider.when_modifying_details;

public class and_details_are_available : given.an_identity_provider_result_handler
{
    TestDetails _originalDetails;
    TestDetails _modifiedDetails;

    void Establish()
    {
        _originalDetails = new TestDetails("Engineering", "Developer");
        _modifiedDetails = new TestDetails("Marketing", "Manager");

        _identityProvider.Provide(Arg.Any<IdentityProviderContext>())
            .Returns(Task.FromResult(new IdentityDetails(true, _originalDetails)));

        var user = CreateAuthenticatedUser();
        _httpRequestContext.User.Returns(user);
        _httpRequestContext.IsHttps.Returns(true);
    }

    async Task Because() => await _handler.ModifyDetails<TestDetails>(details => _modifiedDetails);

    [Fact] void should_set_no_store_cache_control() => _httpRequestContext.Received(1).SetResponseHeader("Cache-Control", "no-store, private");
    [Fact] void should_vary_on_cookie() => _httpRequestContext.Received(1).SetResponseHeader("Vary", "Cookie");

    [Fact] void should_call_write_with_modified_details() =>
        _httpRequestContext.Received(1).Write(Arg.Is<string>(json => json.Contains("Marketing") && json.Contains("Manager")));

    [Fact] void should_not_write_an_identity_cookie() => _httpRequestContext.DidNotReceive().AppendCookie(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CookieOptions>());

    public record TestDetails(string Department, string Role);
}
