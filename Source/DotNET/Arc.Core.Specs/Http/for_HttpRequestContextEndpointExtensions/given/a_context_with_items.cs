// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestContextEndpointExtensions.given;

public class a_context_with_items : Specification
{
    protected IHttpRequestContext _context;

    void Establish()
    {
        _context = Substitute.For<IHttpRequestContext>();
        _context.Items.Returns(new Dictionary<object, object?>());
    }
}
