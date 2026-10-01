// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_DiscoveryExposure;

public class when_a_public_listener_explicitly_requires_unavailable_authentication : given.a_public_listener
{
    Exception _error;

    void Establish() => Configure("Production", "Development", hasHandlers: false, requireAuthentication: true);
    void Because() => _error = Catch.Exception(() => _mapper.Start(_services));

    [Fact] void should_fail_startup() => _error.ShouldBeOfExactType<InvalidIntrospectionConfiguration>();
    [Fact] void should_not_register_discovery() => _mapper.Routes.ShouldBeEmpty();
}
