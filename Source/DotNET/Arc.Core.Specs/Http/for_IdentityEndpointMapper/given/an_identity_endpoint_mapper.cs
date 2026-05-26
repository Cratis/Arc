// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.given;

public class an_identity_endpoint_mapper : Specification
{
    protected IEndpointMapper _mapper;
    protected IServiceProvider _serviceProvider;
    protected IServiceProviderIsService _serviceProviderIsService;
    protected global::Cratis.Types.IInstancesOf<global::Cratis.Arc.Identity.ICanProvideUsers> _usersProviders;
    protected global::Cratis.Types.IInstancesOf<global::Cratis.Arc.Tenancy.ICanProvideTenants> _tenantsProviders;

    void Establish()
    {
        _mapper = Substitute.For<IEndpointMapper>();
        _serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        _usersProviders = Substitute.For<global::Cratis.Types.IInstancesOf<global::Cratis.Arc.Identity.ICanProvideUsers>>();
        _usersProviders.GetEnumerator().Returns(_ => new List<global::Cratis.Arc.Identity.ICanProvideUsers>().GetEnumerator());
        _tenantsProviders = Substitute.For<global::Cratis.Types.IInstancesOf<global::Cratis.Arc.Tenancy.ICanProvideTenants>>();
        _tenantsProviders.GetEnumerator().Returns(_ => new List<global::Cratis.Arc.Tenancy.ICanProvideTenants>().GetEnumerator());

        var services = new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        // Mock GetService to return our substitute
        var mockServiceProvider = Substitute.For<IServiceProvider>();
        mockServiceProvider.GetService(typeof(IServiceProviderIsService)).Returns(_serviceProviderIsService);
        mockServiceProvider.GetService(typeof(global::Cratis.Types.IInstancesOf<global::Cratis.Arc.Identity.ICanProvideUsers>)).Returns(_usersProviders);
        mockServiceProvider.GetService(typeof(global::Cratis.Types.IInstancesOf<global::Cratis.Arc.Tenancy.ICanProvideTenants>)).Returns(_tenantsProviders);
        _serviceProvider = mockServiceProvider;
    }
}
