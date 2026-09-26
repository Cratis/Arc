// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointMapper.when_mapping_introspection_endpoints;

public class with_roles_without_authentication : given.a_introspection_endpoint_mapper
{
    Exception? _failure;

    void Because() => _failure = Catch.Exception(() => _mapper.MapIntrospectionEndpoints(new IntrospectionOptions { Roles = "Administrator" }));

    [Fact] void should_reject_invalid_configuration() => _failure.ShouldBeOfExactType<InvalidIntrospectionConfiguration>();
}
