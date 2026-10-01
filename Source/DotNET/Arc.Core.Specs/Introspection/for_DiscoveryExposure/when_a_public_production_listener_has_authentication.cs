// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Introspection.for_DiscoveryExposure;

public class when_a_public_production_listener_has_authentication : given.a_public_listener
{
    void Establish() => Configure("Production", "Development", hasHandlers: true);
    async Task Because() => await StartAndRequestDiscovery();

    [Fact] void should_require_authentication_on_every_discovery_route() => _statuses.ShouldContainOnly(Enumerable.Repeat(HttpStatusCode.Unauthorized, 5).ToArray());
    [Fact] void should_not_leave_anonymous_discovery_metadata() => _mapper.Routes.All(route => route.Metadata?.RequireAuthentication == true && !route.Metadata.AllowAnonymous).ShouldBeTrue();
}
