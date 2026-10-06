// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointMapper;

public class when_identity_discovery_is_disabled_before_starting_http_listener : given.a_listener_discovery_mapper
{
    void Establish()
    {
        _environment = Environments.Production;
        _options.Introspection.Enabled = false;
        _options.Introspection.RequireAuthentication = true;
        _beforeStarting = () => _options.Introspection.IdentityDiscovery = false;
    }

    void Because() => MapDiscovery();

    [Fact] void should_not_map_deferred_discovery() => _routes.ShouldContainOnly("/.cratis/me");
}
