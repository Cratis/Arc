// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_DiscoveryExposure.when_resolving;

public class outside_development_with_authentication_turned_off : given.a_host
{
    void Because() => Resolve(_mapper, new IntrospectionOptions { RequireAuthentication = false }, isDevelopment: false);

    [Fact] void should_expose_anonymously() => _access.ShouldEqual(DiscoveryAccess.Anonymous);
}
