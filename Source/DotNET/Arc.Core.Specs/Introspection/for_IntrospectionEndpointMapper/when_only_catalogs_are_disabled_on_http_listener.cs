// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointMapper;

public class when_only_catalogs_are_disabled_on_http_listener : given.a_listener_discovery_mapper
{
    void Establish() => _options.Introspection.Enabled = false;
    void Because() => MapDiscovery();

    [Fact] void should_keep_identity_discovery() => _routes.ShouldContainOnly("/.cratis/me", "/.cratis/users", "/.cratis/tenants", "/.cratis/identity-details/schema");
}
