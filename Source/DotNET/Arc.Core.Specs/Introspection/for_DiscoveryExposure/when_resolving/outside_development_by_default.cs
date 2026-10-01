// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_DiscoveryExposure.when_resolving;

public class outside_development_by_default : given.a_host
{
    void Because() => Resolve(_mapper, new IntrospectionOptions(), isDevelopment: false);

    [Fact] void should_require_authentication() => _access.ShouldEqual(DiscoveryAccess.Authenticated);
}
