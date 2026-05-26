// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.given;

public class an_identity_schema_endpoint_handler : Specification
{
    protected IEndpointMapper _mapper;
    protected IHttpRequestContext _httpRequestContext;
    protected Dictionary<string, Func<IHttpRequestContext, Task>> _mappedHandlers;
    protected IServiceCollection _services;
    protected global::Cratis.Types.ITypes _types;

    void Establish()
    {
        _mapper = Substitute.For<IEndpointMapper>();
        _httpRequestContext = Substitute.For<IHttpRequestContext>();
        _mappedHandlers = [];
        _services = new ServiceCollection();
        _types = Substitute.For<global::Cratis.Types.ITypes>();
        _types.FindMultiple<global::Cratis.Arc.Identity.ICanProvideUsers>().Returns([]);
        _types.FindMultiple<global::Cratis.Arc.Tenancy.ICanProvideTenants>().Returns([]);
        _services.AddSingleton<IOptions<ArcOptions>>(Options.Create(new ArcOptions()));

        _httpRequestContext.RequestAborted.Returns(CancellationToken.None);

        _mapper.When(_ => _.MapGet(Arg.Any<string>(), Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Any<EndpointMetadata>()))
            .Do(callInfo => _mappedHandlers[callInfo.ArgAt<string>(0)] = callInfo.ArgAt<Func<IHttpRequestContext, Task>>(1));
    }

    protected void MapIdentityProviderEndpoint()
    {
        var serviceProvider = _services.BuildServiceProvider();
        _httpRequestContext.RequestServices.Returns(serviceProvider);

        var serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        serviceProviderIsService.IsService(typeof(IProvideIdentityDetails)).Returns(true);

        var mockServiceProvider = Substitute.For<IServiceProvider>();
        mockServiceProvider.GetService(typeof(IServiceProviderIsService)).Returns(serviceProviderIsService);
        mockServiceProvider.GetService(typeof(ITypes)).Returns(_types);

        _mapper.MapIdentityProviderEndpoint(mockServiceProvider);
    }
}
