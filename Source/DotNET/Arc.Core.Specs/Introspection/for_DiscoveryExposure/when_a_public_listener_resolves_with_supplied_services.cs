// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_DiscoveryExposure;

public class when_a_public_listener_resolves_with_supplied_services : given.a_public_listener
{
    DiscoveryAccess _access;

    void Establish() => Configure("Production", "Production", hasHandlers: false);
    void Because() => _access = DiscoveryExposure.Resolve(_mapper, new IntrospectionOptions(), _services);

    [Fact] void should_report_discovery_as_unavailable() => _access.ShouldEqual(DiscoveryAccess.Unavailable);
}
