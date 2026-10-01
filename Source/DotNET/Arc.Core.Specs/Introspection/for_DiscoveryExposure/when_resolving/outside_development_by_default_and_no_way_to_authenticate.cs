// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_DiscoveryExposure.when_resolving;

public class outside_development_by_default_and_no_way_to_authenticate : given.a_host
{
    void Because() => Resolve(_mapperThatCannotAuthenticate, new IntrospectionOptions(), isDevelopment: false);

    [Fact] void should_not_fail() => _failure.ShouldBeNull();
    [Fact] void should_leave_the_endpoints_unmapped() => _access.ShouldEqual(DiscoveryAccess.Unavailable);
}
