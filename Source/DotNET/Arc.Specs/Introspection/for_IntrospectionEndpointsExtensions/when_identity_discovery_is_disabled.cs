// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointsExtensions;

[Collection("UsesCurrentDirectory")]
public class when_identity_discovery_is_disabled : given.a_discovery_mapper
{
    void Establish() => _options.Introspection.IdentityDiscovery = false;
    void Because() => MapDiscovery();

    [Fact] void should_not_map_users() => _mapper.EndpointExists("GetUsers").ShouldBeFalse();
    [Fact] void should_not_map_tenants() => _mapper.EndpointExists("GetTenants").ShouldBeFalse();
    [Fact] void should_not_map_the_identity_schema() => _mapper.EndpointExists("GetIdentityDetailsSchema").ShouldBeFalse();
    [Fact] void should_keep_the_command_catalog() => _mapper.EndpointExists("IntrospectCommands").ShouldBeTrue();
    [Fact] void should_keep_the_query_catalog() => _mapper.EndpointExists("IntrospectQueries").ShouldBeTrue();
    [Fact] void should_keep_current_caller_identity() => _mapper.EndpointExists("GetIdentityDetails").ShouldBeTrue();
}
