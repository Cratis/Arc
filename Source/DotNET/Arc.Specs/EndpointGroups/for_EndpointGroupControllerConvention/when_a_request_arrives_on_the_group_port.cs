// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.AspNetCore.EndpointGroups.for_EndpointGroupControllerConvention;

public class when_a_request_arrives_on_the_group_port : given.a_grouped_and_an_ungrouped_controller
{
    async Task Because()
    {
        _httpContext.Connection.LocalPort = InternalPort;
        await _policy.ApplyAsync(_httpContext, _candidates);
    }

    [Fact] void should_match_the_grouped_controller() => IsValid(_groupedEndpoint).ShouldBeTrue();
}
