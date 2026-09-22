// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestContextEndpointExtensions.when_setting_endpoint_metadata;

public class and_metadata_is_provided : given.a_context_with_items
{
    EndpointMetadata _metadata;
    EndpointMetadata? _result;

    void Establish() => _metadata = new EndpointMetadata("TestEndpoint", "Test Endpoint", [], AllowAnonymous: true);

    void Because()
    {
        _context.SetEndpointMetadata(_metadata);
        _result = _context.GetEndpointMetadata();
    }

    [Fact] void should_be_retrievable() => _result.ShouldEqual(_metadata);
}
