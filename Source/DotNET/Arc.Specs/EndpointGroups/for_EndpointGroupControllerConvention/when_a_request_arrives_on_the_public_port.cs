// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.AspNetCore.EndpointGroups.for_EndpointGroupControllerConvention;

public class when_a_request_arrives_on_the_public_port : given.a_grouped_and_an_ungrouped_controller
{
    async Task Because()
    {
        _httpContext.Connection.LocalPort = PublicPort;
        await _policy.ApplyAsync(_httpContext, _candidates);
    }

    [Fact] void should_not_match_the_grouped_controller() => IsValid(_groupedEndpoint).ShouldBeFalse();
    [Fact] void should_match_the_ungrouped_controller() => IsValid(_ungroupedEndpoint).ShouldBeTrue();
}
