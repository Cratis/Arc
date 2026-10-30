// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EndpointGroups.for_EndpointGroupsOptions;

public class when_setting_a_route_prefix : Specification
{
    EndpointGroupsOptions _options;

    void Establish() => _options = new();
    void Because() => _options.SetRoutePrefix("internal", "/internal-api/");

    [Fact] void should_store_the_normalized_prefix() => _options.RoutePrefixes["internal"].ShouldEqual("internal-api");
    [Fact] void should_use_the_group_prefix() => _options.RoutePrefixFor("internal", "api").ShouldEqual("internal-api");
    [Fact] void should_fall_back_for_another_group() => _options.RoutePrefixFor("ops", "api").ShouldEqual("api");
    [Fact] void should_fall_back_for_the_default_group() => _options.RoutePrefixFor(null, "api").ShouldEqual("api");
}
