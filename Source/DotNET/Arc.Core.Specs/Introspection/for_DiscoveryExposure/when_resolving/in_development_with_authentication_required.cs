// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_DiscoveryExposure.when_resolving;

public class in_development_with_authentication_required : given.a_host
{
    void Because() => Resolve(_mapper, new IntrospectionOptions { RequireAuthentication = true }, isDevelopment: true);

    [Fact] void should_require_authentication() => _access.ShouldEqual(DiscoveryAccess.Authenticated);
}
