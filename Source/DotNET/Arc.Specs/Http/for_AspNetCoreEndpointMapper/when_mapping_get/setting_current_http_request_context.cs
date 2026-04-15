// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_mapping_get;

public class setting_current_http_request_context : given.an_endpoint_mapper
{
    const string Pattern = "/test/get-accessor";
    RouteEndpoint _endpoint;
    HttpContext _httpContext;
    IHttpRequestContextAccessor _accessor;
    IHttpRequestContext _contextReceivedByHandler;

    void Establish()
    {
        _accessor = Substitute.For<IHttpRequestContextAccessor>();
        _mapper.MapGet(Pattern, context =>
        {
            _contextReceivedByHandler = context;
            return Task.CompletedTask;
        });

        _endpoint = FindEndpoint(Pattern);

        var serviceProvider = new ServiceCollection()
            .AddSingleton(_accessor)
            .BuildServiceProvider();

        _httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
    }

    async Task Because() => await _endpoint.RequestDelegate!(_httpContext);

    [Fact] void should_set_current_on_the_accessor() => _accessor.Current.ShouldNotBeNull();
    [Fact] void should_set_current_to_the_same_context_passed_to_the_handler() => _accessor.Current.ShouldEqual(_contextReceivedByHandler);
}
