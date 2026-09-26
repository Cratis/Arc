// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointMapper.when_mapping_introspection_endpoints;

public class with_default_exposure : given.a_introspection_endpoint_mapper
{
    void Because() => _mapper.MapIntrospectionEndpoints(new IntrospectionOptions());

    [Fact] void should_map_commands_anonymously() => _mapper.Received(1).MapGet("/.cratis/commands", Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Is<EndpointMetadata>(m => m.AllowAnonymous));
    [Fact] void should_map_queries_anonymously() => _mapper.Received(1).MapGet("/.cratis/queries", Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Is<EndpointMetadata>(m => m.AllowAnonymous));
}
