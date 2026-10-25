// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EndpointGroups.for_EndpointGroupsOptions;

public class when_assigning_a_port_out_of_range : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => new EndpointGroupsOptions().Assign("internal", 70000));

    [Fact] void should_refuse_the_assignment() => _error.ShouldBeOfExactType<InvalidEndpointGroupAssignment>();
}
