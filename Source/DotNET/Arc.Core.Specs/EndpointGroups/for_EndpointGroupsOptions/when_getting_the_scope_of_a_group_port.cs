// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EndpointGroups.for_EndpointGroupsOptions;

public class when_getting_the_scope_of_a_group_port : Specification
{
    EndpointGroupScope _scope;

    void Because() => _scope = new EndpointGroupsOptions().Assign("internal", 5001).ScopeFor(5001);

    [Fact] void should_serve_the_group() => _scope.Serves("internal").ShouldBeTrue();
    [Fact] void should_not_serve_the_default_group() => _scope.Serves(null).ShouldBeFalse();
}
