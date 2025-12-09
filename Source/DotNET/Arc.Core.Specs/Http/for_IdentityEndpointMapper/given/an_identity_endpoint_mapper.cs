// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.given;

public class an_identity_endpoint_mapper : Specification
{
    protected IEndpointMapper _mapper;
    protected IServiceProvider _serviceProvider;
    protected IServiceProviderIsService _serviceProviderIsService;

    void Establish()
    {
        _mapper = Substitute.For<IEndpointMapper>();
        _serviceProviderIsService = Substitute.For<IServiceProviderIsService>();

        var services = new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        // Mock GetService to return our substitute
        var mockServiceProvider = Substitute.For<IServiceProvider>();
        mockServiceProvider.GetService(typeof(IServiceProviderIsService)).Returns(_serviceProviderIsService);
        _serviceProvider = mockServiceProvider;
    }
}
