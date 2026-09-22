// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestContextEndpointExtensions;

public class when_no_endpoint_metadata_was_set : given.a_context_with_items
{
    EndpointMetadata? _metadataResult;
    bool _allowsAnonymousResult;

    void Because()
    {
        _metadataResult = _context.GetEndpointMetadata();
        _allowsAnonymousResult = _context.AllowsAnonymous();
    }

    [Fact] void should_return_null_metadata() => _metadataResult.ShouldBeNull();
    [Fact] void should_not_allow_anonymous() => _allowsAnonymousResult.ShouldBeFalse();
}
