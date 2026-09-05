// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.when_mapping_identity_provider_endpoint;

public class and_identity_provider_is_not_registered : given.an_identity_endpoint_mapper
{
    void Establish()
    {
        _serviceProviderIsService.IsService(typeof(IProvideIdentityDetails)).Returns(false);
    }

    void Because() => _mapper.MapIdentityProviderEndpoint(_serviceProvider);

    [Fact] void should_map_identity_schema_endpoint() => _mapper.Received(1).MapGet(
        "/.cratis/identity-details/schema",
        Arg.Any<Func<IHttpRequestContext, Task>>(),
        Arg.Any<EndpointMetadata>());

    [Fact] void should_map_users_endpoint() => _mapper.Received(1).MapGet(
        "/.cratis/users",
        Arg.Any<Func<IHttpRequestContext, Task>>(),
        Arg.Any<EndpointMetadata>());

    [Fact] void should_map_tenants_endpoint() => _mapper.Received(1).MapGet(
        "/.cratis/tenants",
        Arg.Any<Func<IHttpRequestContext, Task>>(),
        Arg.Any<EndpointMetadata>());

    [Fact] void should_not_map_identity_endpoint() => _mapper.DidNotReceive().MapGet(
        "/.cratis/me",
        Arg.Any<Func<IHttpRequestContext, Task>>(),
        Arg.Any<EndpointMetadata>());
}
