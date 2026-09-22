// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestContextEndpointExtensions;

public class when_setting_endpoint_metadata : Specification
{
    IHttpRequestContext _context;
    EndpointMetadata _metadata;
    EndpointMetadata? _result;

    void Establish()
    {
        _context = Substitute.For<IHttpRequestContext>();
        _context.Items.Returns(new Dictionary<object, object?>());
        _metadata = new EndpointMetadata("TestEndpoint", AllowAnonymous: true);
    }

    void Because()
    {
        _context.SetEndpointMetadata(_metadata);
        _result = _context.GetEndpointMetadata();
    }

    [Fact] void should_round_trip_the_metadata() => _result.ShouldEqual(_metadata);
    [Fact] void should_report_the_endpoint_as_allowing_anonymous_access() => _context.AllowsAnonymous().ShouldBeTrue();
}
