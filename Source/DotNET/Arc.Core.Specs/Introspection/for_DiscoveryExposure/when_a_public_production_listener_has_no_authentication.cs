// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Introspection.for_DiscoveryExposure;

public class when_a_public_production_listener_has_no_authentication : given.a_public_listener
{
    void Establish() => Configure("Production", "Development", hasHandlers: false);
    async Task Because() => await StartAndRequestDiscovery();

    [Fact] void should_leave_every_discovery_route_unmapped() => _statuses.ShouldContainOnly(Enumerable.Repeat(HttpStatusCode.NotFound, 5).ToArray());
    [Fact] void should_not_register_discovery_metadata() => _mapper.Routes.ShouldBeEmpty();
}
