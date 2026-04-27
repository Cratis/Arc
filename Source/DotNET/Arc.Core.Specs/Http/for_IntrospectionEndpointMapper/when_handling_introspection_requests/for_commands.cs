// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http.Introspection;

namespace Cratis.Arc.Http.for_IntrospectionEndpointMapper.when_handling_introspection_requests;

public class for_commands : given.a_introspection_endpoint_mapper
{
    List<CommandIntrospectionMetadata> _discoveredCommands;

    void Establish()
    {
        _mapper.EndpointExists("IntrospectCommands").Returns(false);
        _mapper.EndpointExists("IntrospectQueries").Returns(false);

        _discoveredCommands = [
            new("RegisterOrder", "Features.Orders", "/api/features/orders/register-order", "Features.Orders.RegisterOrder", "")
        ];

        _introspectionService.IntrospectAllEndpoints().Returns((_discoveredCommands, []));
        _mapper.MapIntrospectionEndpoints();
    }

    async Task Because() => await _mappedHandlers["/.cratis/commands"](_httpRequestContext);

    [Fact] void should_write_commands_as_json() => _httpRequestContext.Received(1)
        .WriteResponseAsJson(_discoveredCommands, typeof(List<CommandIntrospectionMetadata>), CancellationToken.None);
}
