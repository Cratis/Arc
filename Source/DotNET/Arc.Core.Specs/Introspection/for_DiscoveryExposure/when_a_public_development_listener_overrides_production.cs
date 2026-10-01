// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_DiscoveryExposure;

public class when_a_public_development_listener_overrides_production : given.a_public_listener
{
    void Establish() => Configure("Development", "Production", hasHandlers: false);
    void Because() => _mapper.Start(_services);

    [Fact] void should_map_all_discovery_routes() => _mapper.Routes.Count().ShouldEqual(5);
    [Fact] void should_allow_anonymous_discovery() => _mapper.Routes.All(route => route.Metadata?.AllowAnonymous == true && !route.Metadata.RequireAuthentication).ShouldBeTrue();
}
