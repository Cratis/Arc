// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_unread_route_below_a_disjoint_prefix : given.a_screen_with_competing_routes
{
    void Establish() => Routes = RouteImports + "\nexport const App = () => <Routes><Route path=\"/authors\"><Route path={destination} element={<Details />} /></Route><Route path=\"/details\" element={<Details />} /></Routes>;";

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_map_the_route_outside_the_unread_subtree() => Result.Source.ShouldContain("item authorDetails navigate to Details.AuthorDetails");
    [Fact] void should_still_report_the_unread_table_once() => Result.Diagnostics.Count(_ => _.Code == "SP0028" && _.Location == RouteFile).ShouldEqual(1);
}
