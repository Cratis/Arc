// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.given;

public class an_identity_endpoint_mapper : Specification
{
    protected IEndpointMapper _mapper;
    protected IServiceProvider _serviceProvider;
    protected IServiceProviderIsService _serviceProviderIsService;
    protected global::Cratis.Types.ITypes _types;

    void Establish()
    {
        _mapper = Substitute.For<IEndpointMapper>();
        _serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        _types = Substitute.For<global::Cratis.Types.ITypes>();
        _types.FindMultiple<global::Cratis.Arc.Identity.ICanProvideUsers>().Returns([]);
        _types.FindMultiple<global::Cratis.Arc.Tenancy.ICanProvideTenants>().Returns([]);

        var services = new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        // Mock GetService to return our substitute
        var mockServiceProvider = Substitute.For<IServiceProvider>();
        mockServiceProvider.GetService(typeof(IServiceProviderIsService)).Returns(_serviceProviderIsService);
        mockServiceProvider.GetService(typeof(ITypes)).Returns(_types);
        _serviceProvider = mockServiceProvider;
    }
}
