// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http.Discovery;

namespace Cratis.Arc.Http.for_DiscoveryEndpointMapper.when_handling_discovery_requests;

public class for_commands : given.a_discovery_endpoint_mapper
{
    List<CommandDiscoveryMetadata> _discoveredCommands;

    void Establish()
    {
        _mapper.EndpointExists("DiscoverCommands").Returns(false);
        _mapper.EndpointExists("DiscoverQueries").Returns(false);

        _discoveredCommands = [
            new("RegisterOrder", "Features.Orders", "/api/features/orders/register-order", "Features.Orders.RegisterOrder", "")
        ];

        _discoveryService.DiscoverAllEndpoints().Returns((_discoveredCommands, []));
        _mapper.MapDiscoveryEndpoints();
    }

    async Task Because() => await _mappedHandlers["/.cratis/commands"](_httpRequestContext);

    [Fact] void should_write_commands_as_json() => _httpRequestContext.Received(1)
        .WriteResponseAsJson(_discoveredCommands, typeof(List<CommandDiscoveryMetadata>), CancellationToken.None);
}
