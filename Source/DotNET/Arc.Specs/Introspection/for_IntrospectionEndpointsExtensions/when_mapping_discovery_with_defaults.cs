// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointsExtensions;

[Collection("UsesCurrentDirectory")]
public class when_mapping_discovery_with_defaults : given.a_discovery_mapper
{
    void Because() => MapDiscovery();

    [Fact] void should_map_users() => _mapper.EndpointExists("GetUsers").ShouldBeTrue();
    [Fact] void should_map_tenants() => _mapper.EndpointExists("GetTenants").ShouldBeTrue();
    [Fact] void should_map_the_identity_schema() => _mapper.EndpointExists("GetIdentityDetailsSchema").ShouldBeTrue();
    [Fact] void should_map_the_command_catalog() => _mapper.EndpointExists("IntrospectCommands").ShouldBeTrue();
    [Fact] void should_map_the_query_catalog() => _mapper.EndpointExists("IntrospectQueries").ShouldBeTrue();
    [Fact] void should_map_current_caller_identity() => _mapper.EndpointExists("GetIdentityDetails").ShouldBeTrue();
}
