// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.when_mapping_identity_provider_endpoint;

public class and_identity_provider_is_registered : given.an_identity_endpoint_mapper
{
    void Establish()
    {
        _serviceProviderIsService.IsService(typeof(IProvideIdentityDetails)).Returns(true);
        _mapper.EndpointExists("GetIdentityDetails").Returns(false);
    }

    void Because() => IdentityEndpointMapper.MapIdentityProviderEndpoint(_mapper, _serviceProvider);

    [Fact] void should_map_get_endpoint() => _mapper.Received(1).MapGet(
        ".cratis/me",
        Arg.Any<Func<IHttpRequestContext, Task>>(),
        Arg.Any<EndpointMetadata>());
}
