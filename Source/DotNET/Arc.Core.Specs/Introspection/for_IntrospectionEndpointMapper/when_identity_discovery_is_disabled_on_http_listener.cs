// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointMapper;

public class when_identity_discovery_is_disabled_on_http_listener : given.a_listener_discovery_mapper
{
    void Establish() => _options.Introspection.IdentityDiscovery = false;
    void Because() => MapDiscovery();

    [Fact] void should_not_map_users() => _routes.ShouldNotContain("/.cratis/users");
    [Fact] void should_not_map_tenants() => _routes.ShouldNotContain("/.cratis/tenants");
    [Fact] void should_not_map_the_identity_schema() => _routes.ShouldNotContain("/.cratis/identity-details/schema");
    [Fact] void should_keep_the_command_catalog() => _routes.ShouldContain("/.cratis/commands");
    [Fact] void should_keep_the_query_catalog() => _routes.ShouldContain("/.cratis/queries");
    [Fact] void should_keep_current_caller_identity() => _routes.ShouldContain("/.cratis/me");
    [Fact] void should_defer_catalog_mapping_until_start() => _routesBeforeStarting.ShouldContainOnly("/.cratis/me");
}
