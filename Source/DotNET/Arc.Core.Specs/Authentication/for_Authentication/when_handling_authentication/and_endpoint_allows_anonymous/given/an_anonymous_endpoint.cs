// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Authentication.for_Authentication.when_handling_authentication.and_endpoint_allows_anonymous.given;

public class an_anonymous_endpoint : for_Authentication.given.an_authentication_system
{
    void Establish()
    {
        _context.Items.Returns(new Dictionary<object, object?>());
        _context.SetEndpointMetadata(new EndpointMetadata("TestEndpoint", AllowAnonymous: true));
    }
}
