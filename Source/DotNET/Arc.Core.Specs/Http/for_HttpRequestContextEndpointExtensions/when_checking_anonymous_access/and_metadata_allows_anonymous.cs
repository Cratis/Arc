// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestContextEndpointExtensions.when_checking_anonymous_access;

public class and_metadata_allows_anonymous : given.a_context_with_items
{
    bool _result;

    void Establish() => _context.SetEndpointMetadata(new EndpointMetadata("TestEndpoint", AllowAnonymous: true));

    void Because() => _result = _context.AllowsAnonymous();

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
