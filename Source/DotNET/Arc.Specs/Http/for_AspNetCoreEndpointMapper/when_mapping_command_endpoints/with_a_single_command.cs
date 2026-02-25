// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;


namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_mapping_command_endpoints;

public class with_a_single_command : given.a_command_endpoint
{
    IReadOnlyList<RouteEndpoint> _endpoints;
    RouteEndpoint _executeEndpoint;
    RouteEndpoint _validateEndpoint;

    void Because()
    {
        _mapper.MapCommandEndpoints(_app.Services);

        _endpoints = GetRouteEndpoints().ToList();
        _executeEndpoint = FindEndpointByName($"Execute{typeof(TestCommand).FullName}");
        _validateEndpoint = FindEndpointByName($"Validate{typeof(TestCommand).FullName}");
    }

    [Fact] void should_register_the_execute_endpoint() => _executeEndpoint.ShouldNotBeNull();
    [Fact] void should_register_the_validate_endpoint() => _validateEndpoint.ShouldNotBeNull();
    [Fact] void should_have_method_info_on_execute_endpoint() => _executeEndpoint.Metadata.GetMetadata<MethodInfo>().ShouldNotBeNull();
    [Fact] void should_have_method_info_on_validate_endpoint() => _validateEndpoint.Metadata.GetMetadata<MethodInfo>().ShouldNotBeNull();
    [Fact] void should_use_post_for_execute_endpoint() => _executeEndpoint.Metadata.GetMetadata<HttpMethodMetadata>().HttpMethods.ShouldContain("POST");
    [Fact] void should_use_post_for_validate_endpoint() => _validateEndpoint.Metadata.GetMetadata<HttpMethodMetadata>().HttpMethods.ShouldContain("POST");
    [Fact] void should_register_two_endpoints() => _endpoints.Count.ShouldEqual(2);
    [Fact] void should_accept_json_on_execute_endpoint() => _executeEndpoint.Metadata.GetMetadata<IAcceptsMetadata>().ContentTypes.ShouldContain("application/json");
    [Fact] void should_accept_json_on_validate_endpoint() => _validateEndpoint.Metadata.GetMetadata<IAcceptsMetadata>().ContentTypes.ShouldContain("application/json");
    [Fact] void should_have_command_type_as_accepted_request_type_on_execute() => _executeEndpoint.Metadata.GetMetadata<IAcceptsMetadata>().RequestType.ShouldEqual(typeof(TestCommand));
    [Fact] void should_have_command_type_as_accepted_request_type_on_validate() => _validateEndpoint.Metadata.GetMetadata<IAcceptsMetadata>().RequestType.ShouldEqual(typeof(TestCommand));
    [Fact] void should_produce_json_on_execute_endpoint() => _executeEndpoint.Metadata.GetMetadata<IProducesResponseTypeMetadata>().Type.ShouldEqual(typeof(CommandResult));
    [Fact] void should_produce_json_on_validate_endpoint() => _validateEndpoint.Metadata.GetMetadata<IProducesResponseTypeMetadata>().Type.ShouldEqual(typeof(CommandResult));
}
