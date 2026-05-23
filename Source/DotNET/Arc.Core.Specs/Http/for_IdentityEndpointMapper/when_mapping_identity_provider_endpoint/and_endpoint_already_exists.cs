// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.when_mapping_identity_provider_endpoint;

public class and_endpoint_already_exists : given.an_identity_endpoint_mapper
{
    void Establish()
    {
        _serviceProviderIsService.IsService(typeof(IProvideIdentityDetails)).Returns(true);
        _mapper.EndpointExists("GetIdentityDetailsSchema").Returns(true);
        _mapper.EndpointExists("GetIdentityDetails").Returns(true);
    }

    void Because() => _mapper.MapIdentityProviderEndpoint(_serviceProvider);

    [Fact] void should_not_map_duplicate_identity_endpoint() => _mapper.DidNotReceive().MapGet(
        "/.cratis/me",
        Arg.Any<Func<IHttpRequestContext, Task>>(),
        Arg.Any<EndpointMetadata>());

    [Fact] void should_not_map_duplicate_schema_endpoint() => _mapper.DidNotReceive().MapGet(
        "/.cratis/identity-details/schema",
        Arg.Any<Func<IHttpRequestContext, Task>>(),
        Arg.Any<EndpointMetadata>());
}
