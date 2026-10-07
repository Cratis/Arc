// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointMapper;

public class when_all_discovery_is_disabled_on_http_listener : given.a_listener_discovery_mapper
{
    void Establish()
    {
        _environment = Environments.Production;
        _options.Introspection = new IntrospectionOptions
        {
            Enabled = false,
            IdentityDiscovery = false,
            RequireAuthentication = true,
            Roles = "Administrator"
        };
    }

    void Because() => MapDiscovery();

    [Fact] void should_map_only_current_caller_identity() => _routes.ShouldContainOnly("/.cratis/me");
    [Fact] void should_keep_current_caller_identity_before_start() => _routesBeforeStarting.ShouldContainOnly("/.cratis/me");
}
