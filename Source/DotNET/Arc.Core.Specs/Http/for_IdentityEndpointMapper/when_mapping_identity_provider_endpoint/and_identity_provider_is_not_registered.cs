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

    [Fact] void should_not_map_endpoint() => _mapper.DidNotReceive().MapGet(
        Arg.Any<string>(),
        Arg.Any<Func<IHttpRequestContext, Task>>(),
        Arg.Any<EndpointMetadata>());
}
