// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http.Introspection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_IntrospectionEndpointMapper.given;

public class a_introspection_endpoint_mapper : Specification
{
    protected IEndpointMapper _mapper;
    protected IIntrospectionService _introspectionService;
    protected IHttpRequestContext _httpRequestContext;
    protected Dictionary<string, Func<IHttpRequestContext, Task>> _mappedHandlers;

    void Establish()
    {
        _mapper = Substitute.For<IEndpointMapper>();
        _introspectionService = Substitute.For<IIntrospectionService>();
        _httpRequestContext = Substitute.For<IHttpRequestContext>();
        _mappedHandlers = [];

        var services = new ServiceCollection();
        services.AddSingleton(_introspectionService);
        _httpRequestContext.RequestServices.Returns(services.BuildServiceProvider());
        _httpRequestContext.RequestAborted.Returns(CancellationToken.None);

        _mapper.When(_ => _.MapGet(Arg.Any<string>(), Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Any<EndpointMetadata>()))
            .Do(callInfo => _mappedHandlers[callInfo.ArgAt<string>(0)] = callInfo.ArgAt<Func<IHttpRequestContext, Task>>(1));
    }
}
