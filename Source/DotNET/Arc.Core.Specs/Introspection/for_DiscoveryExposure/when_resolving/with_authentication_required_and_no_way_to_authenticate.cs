// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_DiscoveryExposure.when_resolving;

public class with_authentication_required_and_no_way_to_authenticate : given.a_host
{
    void Because() => Resolve(_mapperThatCannotAuthenticate, new IntrospectionOptions { RequireAuthentication = true }, isDevelopment: false);

    [Fact] void should_fail_at_startup() => _failure.ShouldBeOfExactType<InvalidIntrospectionConfiguration>();
}
