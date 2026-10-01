// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Introspection.for_DiscoveryExposure;

[Collection("UsesCurrentDirectory")]
public class when_a_production_listener_has_no_authentication_handlers : given.a_listener_host
{
    Task Because() => RequestDiscovery(["--environment=Production"]);

    [Fact] void should_leave_all_discovery_routes_unmapped() => _statuses.ShouldContainOnly(Enumerable.Repeat(HttpStatusCode.NotFound, 5).ToArray());
    [Fact] void should_warn_how_to_configure_discovery() => _warnings.Any(message => message.Contains("are not mapped", StringComparison.Ordinal) && message.Contains("Cratis:Arc:Introspection:RequireAuthentication", StringComparison.Ordinal)).ShouldBeTrue();
}
