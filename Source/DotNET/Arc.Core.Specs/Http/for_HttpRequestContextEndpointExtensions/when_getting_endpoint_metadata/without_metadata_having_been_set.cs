// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestContextEndpointExtensions.when_getting_endpoint_metadata;

public class without_metadata_having_been_set : Specification
{
    IHttpRequestContext _context;
    EndpointMetadata? _result;

    void Establish() => _context = Substitute.For<IHttpRequestContext>();

    void Because() => _result = _context.GetEndpointMetadata();

    [Fact] void should_return_null() => _result.ShouldBeNull();
    [Fact] void should_report_the_endpoint_as_not_allowing_anonymous_access() => _context.AllowsAnonymous().ShouldBeFalse();
}
