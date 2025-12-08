// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.given;

public class an_identity_endpoint_handler : Specification
{
    protected IEndpointMapper _mapper;
    protected IServiceProvider _serviceProvider;
    protected IServiceProviderIsService _serviceProviderIsService;
    protected IIdentityProviderResultHandler _identityProviderResultHandler;
    protected IHttpRequestContext _httpRequestContext;
    protected Func<IHttpRequestContext, Task> _capturedHandler;

    void Establish()
    {
        _mapper = Substitute.For<IEndpointMapper>();
        _serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        _identityProviderResultHandler = Substitute.For<IIdentityProviderResultHandler>();
        _httpRequestContext = Substitute.For<IHttpRequestContext>();

        var services = new ServiceCollection();
        services.AddSingleton(_identityProviderResultHandler);
        _serviceProvider = services.BuildServiceProvider();

        var mockServiceProvider = Substitute.For<IServiceProvider>();
        mockServiceProvider.GetService(typeof(IServiceProviderIsService)).Returns(_serviceProviderIsService);
        mockServiceProvider.GetService(typeof(IIdentityProviderResultHandler)).Returns(_identityProviderResultHandler);

        _httpRequestContext.RequestServices.Returns(_serviceProvider);

        _serviceProviderIsService.IsService(typeof(IProvideIdentityDetails)).Returns(true);

        _mapper.When(_ => _.MapGet(Arg.Any<string>(), Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Any<EndpointMetadata>()))
               .Do(callInfo => _capturedHandler = callInfo.ArgAt<Func<IHttpRequestContext, Task>>(1));

        IdentityEndpointMapper.MapIdentityProviderEndpoint(_mapper, mockServiceProvider);
    }
}
