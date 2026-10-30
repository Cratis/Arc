// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.EndpointGroups.for_EndpointRouteHelper;

public class when_building_a_route_with_a_group_prefix : Specification
{
    string _route;

    void Because() => _route = EndpointRouteHelper.BuildRouteUrl("/internal-api/", ["App", "Orders"], 1, "GetAll", true);

    [Fact] void should_use_the_explicit_prefix() => _route.ShouldEqual("/internal-api/orders/get-all");
    [Fact] void should_keep_the_original_options_overload() => typeof(EndpointRouteHelper)
        .GetMethod(nameof(EndpointRouteHelper.BuildRouteUrl), [typeof(ApiEndpointOptions), typeof(IEnumerable<string>), typeof(int), typeof(string), typeof(bool)])
        .ShouldNotBeNull();
}
