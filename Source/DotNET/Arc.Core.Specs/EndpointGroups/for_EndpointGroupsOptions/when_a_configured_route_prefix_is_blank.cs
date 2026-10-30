// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EndpointGroups.for_EndpointGroupsOptions;

public class when_a_configured_route_prefix_is_blank : Specification
{
    EndpointGroupsOptions _options;
    string _prefix;

    void Establish()
    {
        _options = new();
        _options.RoutePrefixes["internal"] = " ";
    }

    void Because() => _prefix = _options.RoutePrefixFor("internal", "api");

    [Fact] void should_fall_back_to_the_global_prefix() => _prefix.ShouldEqual("api");
}
