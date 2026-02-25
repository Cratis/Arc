// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_mapping_post;

public class with_handler : given.an_endpoint_mapper
{
    const string Pattern = "/test/post";
    RouteEndpoint _endpoint;

    void Establish()
    {
        _mapper.MapPost(Pattern, _ => Task.CompletedTask);
    }

    void Because()
    {
        _endpoint = FindEndpoint(Pattern);
    }

    [Fact] void should_register_the_endpoint() => _endpoint.ShouldNotBeNull();
    [Fact] void should_have_method_info_metadata() => _endpoint.Metadata.GetMetadata<MethodInfo>().ShouldNotBeNull();
    [Fact] void should_have_post_http_method() => _endpoint.Metadata.GetMetadata<HttpMethodMetadata>().HttpMethods.ShouldContain("POST");
}
