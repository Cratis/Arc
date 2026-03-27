// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexerEndpointMapper;

public class when_mapping_endpoints : Specification
{
    IEndpointMapper _mapper;
    List<(string Route, string Method, bool AllowAnonymous)> _mappedEndpoints;

    void Establish()
    {
        _mapper = Substitute.For<IEndpointMapper>();
        _mapper.EndpointExists(Arg.Any<string>()).Returns(false);
        _mappedEndpoints = [];

        _mapper.When(_ => _.MapGet(Arg.Any<string>(), Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Any<EndpointMetadata>()))
            .Do(ci => _mappedEndpoints.Add((ci.ArgAt<string>(0), "GET", ci.ArgAt<EndpointMetadata>(2).AllowAnonymous)));

        _mapper.When(_ => _.MapPost(Arg.Any<string>(), Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Any<EndpointMetadata>()))
            .Do(ci => _mappedEndpoints.Add((ci.ArgAt<string>(0), "POST", ci.ArgAt<EndpointMetadata>(2).AllowAnonymous)));
    }

    void Because() => _mapper.MapObservableQueryDemultiplexerEndpoints(Substitute.For<IServiceProvider>());

    [Fact] void should_map_four_endpoints() => _mappedEndpoints.Count.ShouldEqual(4);
    [Fact] void should_map_websocket_get() => _mappedEndpoints.ShouldContain((ObservableQueryDemultiplexerEndpointMapper.WebSocketRoute, "GET", true));
    [Fact] void should_map_sse_get() => _mappedEndpoints.ShouldContain((ObservableQueryDemultiplexerEndpointMapper.SseRoute, "GET", true));
    [Fact] void should_map_sse_subscribe_post() => _mappedEndpoints.ShouldContain((ObservableQueryDemultiplexerEndpointMapper.SseSubscribeRoute, "POST", true));
    [Fact] void should_map_sse_unsubscribe_post() => _mappedEndpoints.ShouldContain((ObservableQueryDemultiplexerEndpointMapper.SseUnsubscribeRoute, "POST", true));
}
