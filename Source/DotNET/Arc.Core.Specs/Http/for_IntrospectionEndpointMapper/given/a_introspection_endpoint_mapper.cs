// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http.Discovery;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_DiscoveryEndpointMapper.given;

public class a_discovery_endpoint_mapper : Specification
{
    protected IEndpointMapper _mapper;
    protected IDiscoveryService _discoveryService;
    protected IHttpRequestContext _httpRequestContext;
    protected Dictionary<string, Func<IHttpRequestContext, Task>> _mappedHandlers;

    void Establish()
    {
        _mapper = Substitute.For<IEndpointMapper>();
        _discoveryService = Substitute.For<IDiscoveryService>();
        _httpRequestContext = Substitute.For<IHttpRequestContext>();
        _mappedHandlers = [];

        var services = new ServiceCollection();
        services.AddSingleton(_discoveryService);
        _httpRequestContext.RequestServices.Returns(services.BuildServiceProvider());
        _httpRequestContext.RequestAborted.Returns(CancellationToken.None);

        _mapper.When(_ => _.MapGet(Arg.Any<string>(), Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Any<EndpointMetadata>()))
            .Do(callInfo => _mappedHandlers[callInfo.ArgAt<string>(0)] = callInfo.ArgAt<Func<IHttpRequestContext, Task>>(1));
    }
}
