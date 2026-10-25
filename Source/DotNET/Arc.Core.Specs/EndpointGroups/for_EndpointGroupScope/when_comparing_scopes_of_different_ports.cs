// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EndpointGroups.for_EndpointGroupScope;

public class when_comparing_scopes_of_different_ports : Specification
{
    bool _result;

    void Because() => _result = EndpointGroupScope.Default.IsSameAs(new EndpointGroupScope(false, ["internal"]));

    [Fact] void should_not_be_the_same() => _result.ShouldBeFalse();
}
