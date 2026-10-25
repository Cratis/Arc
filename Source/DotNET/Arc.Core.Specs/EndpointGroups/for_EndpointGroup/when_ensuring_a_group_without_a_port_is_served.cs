// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EndpointGroups.for_EndpointGroup;

public class when_ensuring_a_group_without_a_port_is_served : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => EndpointGroup.EnsureServed(typeof(InternalCommand), new EndpointGroupsOptions()));

    [Fact] void should_refuse_to_serve_it() => _error.ShouldBeOfExactType<EndpointGroupHasNoPort>();

    [EndpointGroup("internal")]
    public record InternalCommand;
}
