// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Http.for_AuthenticationMiddleware.when_authenticating;

public class and_user_is_not_authenticated : given.an_authentication_middleware
{
    bool _result;

    void Establish()
    {
        _metadata = new EndpointMetadata("TestEndpoint", "Test Endpoint", [], AllowAnonymous: false);
        _authentication.HandleAuthentication(_httpRequestContext).Returns(Task.FromResult(AuthenticationResult.Anonymous));
    }

    async Task Because() => _result = await _middleware.AuthenticateAsync(_httpRequestContext, _metadata);

    [Fact] void should_return_false() => _result.ShouldBeFalse();
    [Fact] void should_call_authentication() => _authentication.Received(1).HandleAuthentication(_httpRequestContext);
    [Fact] void should_set_status_code_to_unauthorized() => _httpRequestContext.Received().SetStatusCode(401);
    [Fact] void should_write_unauthorized_message() => _httpRequestContext.Received().WriteAsync("Unauthorized", Arg.Any<CancellationToken>());
}
