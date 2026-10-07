// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointMapper;

public class when_mapping_discovery_with_defaults_on_http_listener : given.a_listener_discovery_mapper
{
    void Because() => MapDiscovery();

    [Fact] void should_map_users() => _routes.ShouldContain("/.cratis/users");
    [Fact] void should_map_tenants() => _routes.ShouldContain("/.cratis/tenants");
    [Fact] void should_map_the_identity_schema() => _routes.ShouldContain("/.cratis/identity-details/schema");
    [Fact] void should_map_the_command_catalog() => _routes.ShouldContain("/.cratis/commands");
    [Fact] void should_map_the_query_catalog() => _routes.ShouldContain("/.cratis/queries");
    [Fact] void should_map_current_caller_identity() => _routes.ShouldContain("/.cratis/me");
    [Fact] void should_defer_discovery_mapping_until_start() => _routesBeforeStarting.ShouldContainOnly("/.cratis/me");
}
