// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointMapper.when_mapping_introspection_endpoints;

public class with_trust_without_enabled_authentication : given.a_introspection_endpoint_mapper
{
    Exception? _failure;

    void Because() => _failure = Catch.Exception(() => _mapper.MapIntrospectionEndpoints(new IntrospectionOptions { Enabled = false, RequireAuthentication = true, TrustForwardedIdentityHeaders = true }));

    [Fact] void should_reject_trust_when_catalog_is_disabled() => _failure.ShouldBeOfExactType<InvalidIntrospectionConfiguration>();
}
