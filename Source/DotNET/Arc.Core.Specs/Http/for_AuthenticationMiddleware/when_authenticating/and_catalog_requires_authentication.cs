// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Authentication;

namespace Cratis.Arc.Http.for_AuthenticationMiddleware.when_authenticating;

public class and_catalog_requires_authentication : given.an_authentication_middleware
{
    bool _result;

    void Establish()
    {
        _metadata = new EndpointMetadata("Catalog") { RequireAuthentication = true };
        _authentication.HandleAuthentication(_httpRequestContext).Returns(AuthenticationResult.Anonymous);
    }

    async Task Because() => _result = await _middleware.Authenticate(_httpRequestContext, _metadata);

    [Fact] void should_reject_anonymous_request() => _result.ShouldBeFalse();
    [Fact] void should_return_unauthorized() => _httpRequestContext.Received(1).SetStatusCode(HttpStatusCode.Unauthorized);
}
