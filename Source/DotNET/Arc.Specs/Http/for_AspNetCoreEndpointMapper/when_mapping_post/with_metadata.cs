// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_mapping_post;

public class with_metadata : given.an_endpoint_mapper
{
    const string Pattern = "/test/post-meta";
    const string EndpointName = "TestPostEndpoint";
    const string Summary = "A test POST endpoint";
    RouteEndpoint _endpoint;

    void Establish()
    {
        var metadata = new EndpointMetadata(EndpointName, Summary, ["TestTag"], false);
        _mapper.MapPost(Pattern, _ => Task.CompletedTask, metadata);
    }

    void Because()
    {
        _endpoint = FindEndpoint(Pattern);
    }

    [Fact] void should_register_the_endpoint() => _endpoint.ShouldNotBeNull();
    [Fact] void should_have_method_info_metadata() => _endpoint.Metadata.GetMetadata<MethodInfo>().ShouldNotBeNull();
    [Fact] void should_have_the_endpoint_name() => _endpoint.Metadata.GetMetadata<IEndpointNameMetadata>().EndpointName.ShouldEqual(EndpointName);
}
