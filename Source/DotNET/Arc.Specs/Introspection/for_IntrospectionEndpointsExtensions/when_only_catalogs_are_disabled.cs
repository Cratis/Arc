// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointsExtensions;

[Collection("UsesCurrentDirectory")]
public class when_only_catalogs_are_disabled : given.a_discovery_mapper
{
    void Establish() => _options.Introspection.Enabled = false;
    void Because() => MapDiscovery();

    [Fact] void should_keep_users() => _mapper.EndpointExists("GetUsers").ShouldBeTrue();
    [Fact] void should_keep_tenants() => _mapper.EndpointExists("GetTenants").ShouldBeTrue();
    [Fact] void should_keep_the_identity_schema() => _mapper.EndpointExists("GetIdentityDetailsSchema").ShouldBeTrue();
    [Fact] void should_not_map_the_command_catalog() => _mapper.EndpointExists("IntrospectCommands").ShouldBeFalse();
    [Fact] void should_not_map_the_query_catalog() => _mapper.EndpointExists("IntrospectQueries").ShouldBeFalse();
}
