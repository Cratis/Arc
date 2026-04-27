// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http.Discovery;

namespace Cratis.Arc.Http.for_DiscoveryEndpointMapper.when_handling_discovery_requests;

public class for_queries : given.a_discovery_endpoint_mapper
{
    List<QueryDiscoveryMetadata> _discoveredQueries;

    void Establish()
    {
        _mapper.EndpointExists("DiscoverCommands").Returns(false);
        _mapper.EndpointExists("DiscoverQueries").Returns(false);

        _discoveredQueries = [
            new("AllOrders", "Features.Orders", "/api/features/orders/all-orders", "Features.Orders.AllOrders", "")
        ];

        _discoveryService.DiscoverAllEndpoints().Returns(([], _discoveredQueries));
        _mapper.MapDiscoveryEndpoints();
    }

    async Task Because() => await _mappedHandlers["/.cratis/queries"](_httpRequestContext);

    [Fact] void should_write_queries_as_json() => _httpRequestContext.Received(1)
        .WriteResponseAsJson(_discoveredQueries, typeof(List<QueryDiscoveryMetadata>), CancellationToken.None);
}
