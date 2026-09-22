// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestContextEndpointExtensions.when_setting_endpoint_metadata;

public class and_metadata_is_null : given.a_context_with_items
{
    EndpointMetadata? _result;

    void Because()
    {
        _context.SetEndpointMetadata(null);
        _result = _context.GetEndpointMetadata();
    }

    [Fact] void should_return_null() => _result.ShouldBeNull();
}
