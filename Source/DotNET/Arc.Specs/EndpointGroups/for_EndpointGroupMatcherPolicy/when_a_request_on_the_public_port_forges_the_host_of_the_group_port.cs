// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.AspNetCore.EndpointGroups.for_EndpointGroupMatcherPolicy;

public class when_a_request_on_the_public_port_forges_the_host_of_the_group_port : given.candidates_on_a_port
{
    async Task Because()
    {
        _httpContext.Connection.LocalPort = PublicPort;
        _httpContext.Request.Host = new HostString("localhost", InternalPort);
        await _policy.ApplyAsync(_httpContext, _candidates);
    }

    [Fact] void should_drop_the_grouped_endpoint() => IsValid(_internalEndpoint).ShouldBeFalse();
}
