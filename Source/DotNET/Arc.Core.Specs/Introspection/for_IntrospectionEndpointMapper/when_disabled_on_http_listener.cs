// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Http.for_HttpListenerEndpointMapper.given;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointMapper;

public class when_disabled_on_http_listener : a_running_endpoint_mapper
{
    HttpStatusCode _commandsStatus;
    HttpStatusCode _queriesStatus;

    async Task Because()
    {
        _endpointMapper.MapIntrospectionEndpoints(new IntrospectionOptions { Enabled = false });
        StartEndpointMapper();
        _commandsStatus = (await _httpClient.GetAsync("/.cratis/commands")).StatusCode;
        _queriesStatus = (await _httpClient.GetAsync("/.cratis/queries")).StatusCode;
    }

    [Fact] void should_not_serve_commands() => _commandsStatus.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_not_serve_queries() => _queriesStatus.ShouldEqual(HttpStatusCode.NotFound);
}
