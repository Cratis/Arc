// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authentication.for_Authentication.when_handling_authentication;

public class without_handlers : given.an_authentication_system
{
    AuthenticationResult _result;

    void Establish()
    {
        _handlers.GetEnumerator().Returns(Enumerable.Empty<IAuthenticationHandler>().GetEnumerator());
    }

    async Task Because() => _result = await _authentication.HandleAuthentication(_context);

    [Fact] void should_return_anonymous_result() => _result.ShouldEqual(AuthenticationResult.Anonymous);
}
