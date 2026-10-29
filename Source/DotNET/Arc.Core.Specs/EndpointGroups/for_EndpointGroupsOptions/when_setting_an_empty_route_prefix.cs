// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EndpointGroups.for_EndpointGroupsOptions;

public class when_setting_an_empty_route_prefix : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => new EndpointGroupsOptions().SetRoutePrefix("internal", " "));

    [Fact] void should_refuse_the_assignment() => _error.ShouldBeOfExactType<InvalidEndpointGroupAssignment>();
}
