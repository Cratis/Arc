// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_route_table_in_a_local_layout : given.a_screen_with_competing_routes
{
    void Establish() => Routes = RouteImports + """

        const AuthorRoutes = () => <Routes><Route path="details" element={<Details />} /></Routes>;
        export const App = () => <Routes><Route path="/authors/*" element={<AuthorRoutes />} /></Routes>;
        """;

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_not_treat_a_local_layout_table_as_a_root() => Result.Source.Contains("navigate to", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_unknown_mount_once() => Result.Diagnostics.Count(_ => _.Code == "SP0028" && _.Location == RouteFile && _.Message.Contains("unknown parent prefix", StringComparison.Ordinal)).ShouldEqual(1);
}
