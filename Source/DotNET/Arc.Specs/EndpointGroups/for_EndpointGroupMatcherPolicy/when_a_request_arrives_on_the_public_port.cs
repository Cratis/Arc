// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.AspNetCore.EndpointGroups.for_EndpointGroupMatcherPolicy;

public class when_a_request_arrives_on_the_public_port : given.candidates_on_a_port
{
    async Task Because()
    {
        _httpContext.Connection.LocalPort = PublicPort;
        await _policy.ApplyAsync(_httpContext, _candidates);
    }

    [Fact] void should_keep_the_default_endpoint() => IsValid(_defaultEndpoint).ShouldBeTrue();
    [Fact] void should_drop_the_grouped_endpoint() => IsValid(_internalEndpoint).ShouldBeFalse();
    [Fact] void should_leave_endpoints_that_are_not_arcs_alone() => IsValid(_foreignEndpoint).ShouldBeTrue();
}
