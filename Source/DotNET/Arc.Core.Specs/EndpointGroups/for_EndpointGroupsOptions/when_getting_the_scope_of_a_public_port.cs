// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EndpointGroups.for_EndpointGroupsOptions;

public class when_getting_the_scope_of_a_public_port : Specification
{
    EndpointGroupScope _scope;

    void Because() => _scope = new EndpointGroupsOptions().Assign("internal", 5001).ScopeFor(5000);

    [Fact] void should_serve_the_default_group() => _scope.Serves(null).ShouldBeTrue();
    [Fact] void should_not_serve_the_named_group() => _scope.Serves("internal").ShouldBeFalse();
}
