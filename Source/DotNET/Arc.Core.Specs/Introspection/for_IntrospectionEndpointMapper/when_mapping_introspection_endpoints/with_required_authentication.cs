// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointMapper.when_mapping_introspection_endpoints;

public class with_required_authentication : given.a_introspection_endpoint_mapper
{
    void Because() => _mapper.MapIntrospectionEndpoints(new IntrospectionOptions { RequireAuthentication = true, Roles = "Administrator,Operator" });

    [Fact] void should_require_authentication_for_commands() => _mapper.Received(1).MapGet("/.cratis/commands", Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Is<EndpointMetadata>(m => !m.AllowAnonymous && m.RequireAuthentication && m.Roles == "Administrator,Operator"));
    [Fact] void should_require_authentication_for_queries() => _mapper.Received(1).MapGet("/.cratis/queries", Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Is<EndpointMetadata>(m => !m.AllowAnonymous && m.RequireAuthentication && m.Roles == "Administrator,Operator"));
}
