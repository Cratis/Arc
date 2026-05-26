// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.when_mapping_identity_provider_endpoint;

public class and_tenants_provider_is_registered : given.an_identity_endpoint_mapper
{
    void Establish()
    {
        _serviceProviderIsService.IsService(typeof(IProvideIdentityDetails)).Returns(true);
        _mapper.EndpointExists("GetIdentityDetailsSchema").Returns(false);
        _mapper.EndpointExists("GetIdentityDetails").Returns(false);
        _mapper.EndpointExists("GetTenants").Returns(false);
        _types.FindMultiple<ICanProvideTenants>().Returns([typeof(TenantsProvider)]);
    }

    void Because() => _mapper.MapIdentityProviderEndpoint(_serviceProvider);

    [Fact] void should_map_tenants_endpoint() => _mapper.Received(1).MapGet(
        "/.cratis/tenants",
        Arg.Any<Func<IHttpRequestContext, Task>>(),
        Arg.Any<EndpointMetadata>());

    class TenantsProvider : ICanProvideTenants
    {
        public Task<IEnumerable<Tenant>> Provide() => Task.FromResult<IEnumerable<Tenant>>([]);
    }
}
