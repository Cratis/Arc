// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.when_handling_tenants_request;

public class and_tenants_are_provided : given.an_identity_schema_endpoint_handler
{
    void Establish()
    {
        _types.FindMultiple<ICanProvideTenants>().Returns([typeof(FirstProvider), typeof(SecondProvider)]);
        MapIdentityProviderEndpoint();
    }

    async Task Because() => await _mappedHandlers["/.cratis/tenants"](_httpRequestContext);

    [Fact]
    void should_write_tenants_from_all_providers()
    {
        _httpRequestContext.Received(1).WriteResponseAsJson(
            Arg.Is<object>(value =>
                value != null &&
                ((IEnumerable<Tenant>)value).Count() == 2 &&
                ((IEnumerable<Tenant>)value).Any(_ => _.Id == "first" && _.Name == "First Tenant") &&
                ((IEnumerable<Tenant>)value).Any(_ => _.Id == "second" && _.Name == "Second Tenant")),
            typeof(IEnumerable<Tenant>),
            CancellationToken.None);
    }

    class FirstProvider : ICanProvideTenants
    {
        public Task<IEnumerable<Tenant>> Provide(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Tenant>>([new Tenant("first", "First Tenant")]);
    }

    class SecondProvider : ICanProvideTenants
    {
        public Task<IEnumerable<Tenant>> Provide(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Tenant>>([new Tenant("second", "Second Tenant")]);
    }
}
