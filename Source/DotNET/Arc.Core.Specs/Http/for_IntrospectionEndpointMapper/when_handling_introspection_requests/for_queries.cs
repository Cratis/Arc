// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http.Introspection;

namespace Cratis.Arc.Http.for_IntrospectionEndpointMapper.when_handling_introspection_requests;

public class for_queries : given.a_introspection_endpoint_mapper
{
    List<QueryIntrospectionMetadata> _discoveredQueries;

    void Establish()
    {
        _mapper.EndpointExists("IntrospectCommands").Returns(false);
        _mapper.EndpointExists("IntrospectQueries").Returns(false);

        _discoveredQueries = [
            new("AllOrders", "Features.Orders", "/api/features/orders/all-orders", "Features.Orders.AllOrders", "")
        ];

        _introspectionService.IntrospectAllEndpoints().Returns(([], _discoveredQueries));
        _mapper.MapIntrospectionEndpoints();
    }

    async Task Because() => await _mappedHandlers["/.cratis/queries"](_httpRequestContext);

    [Fact] void should_write_queries_as_json() => _httpRequestContext.Received(1)
        .WriteResponseAsJson(_discoveredQueries, typeof(List<QueryIntrospectionMetadata>), CancellationToken.None);
}
