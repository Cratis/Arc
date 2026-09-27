// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointMapper.when_mapping_introspection_endpoints;

public class with_exposure_options : given.a_introspection_endpoint_mapper
{
    void Because() => _mapper.MapIntrospectionEndpoints(new IntrospectionOptions { Enabled = false });

    [Fact] void should_not_map_commands() => _mappedHandlers.ContainsKey("/.cratis/commands").ShouldBeFalse();
    [Fact] void should_not_map_queries() => _mappedHandlers.ContainsKey("/.cratis/queries").ShouldBeFalse();
}
