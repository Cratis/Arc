// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http.Discovery;

namespace Cratis.Arc.Http.for_DiscoveryEndpointMapper.when_mapping_discovery_endpoints;

public class with_existing_endpoints : given.a_discovery_endpoint_mapper
{
    void Establish()
    {
        _mapper.EndpointExists("DiscoverCommands").Returns(true);
        _mapper.EndpointExists("DiscoverQueries").Returns(true);
    }

    void Because() => _mapper.MapDiscoveryEndpoints();

    [Fact] void should_not_map_commands_endpoint() => _mapper.DidNotReceive().MapGet("/.cratis/commands", Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Any<EndpointMetadata>());
    [Fact] void should_not_map_queries_endpoint() => _mapper.DidNotReceive().MapGet("/.cratis/queries", Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Any<EndpointMetadata>());
}
