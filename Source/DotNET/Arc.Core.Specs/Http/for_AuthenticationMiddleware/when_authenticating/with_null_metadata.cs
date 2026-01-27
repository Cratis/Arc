// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Http.for_AuthenticationMiddleware.when_authenticating;

public class with_null_metadata : given.an_authentication_middleware
{
    bool _result;

    void Establish()
    {
        _authentication.HandleAuthentication(_httpRequestContext).Returns(Task.FromResult(AuthenticationResult.Anonymous));
    }

    async Task Because() => _result = await _middleware.Authenticate(_httpRequestContext, null);

    [Fact] void should_return_false() => _result.ShouldBeFalse();
    [Fact] void should_call_authentication() => _authentication.Received(1).HandleAuthentication(_httpRequestContext);
    [Fact] void should_set_status_code_to_unauthorized() => _httpRequestContext.Received().SetStatusCode(401);
}
