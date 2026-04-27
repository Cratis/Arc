// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http.Introspection;

namespace Cratis.Arc.Http.for_IntrospectionEndpointMapper.when_mapping_introspection_endpoints;

public class with_no_existing_endpoints : given.a_introspection_endpoint_mapper
{
    void Establish()
    {
        _mapper.EndpointExists("IntrospectCommands").Returns(false);
        _mapper.EndpointExists("IntrospectQueries").Returns(false);
    }

    void Because() => _mapper.MapIntrospectionEndpoints();

    [Fact] void should_map_commands_endpoint() => _mapper.Received(1).MapGet("/.cratis/commands", Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Any<EndpointMetadata>());
    [Fact] void should_map_queries_endpoint() => _mapper.Received(1).MapGet("/.cratis/queries", Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Any<EndpointMetadata>());
}
