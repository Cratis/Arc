// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_nested_routes_in_the_same_file : given.a_screen_with_competing_routes
{
    void Establish() => Routes = RouteImports + """

        export const App = () => <Routes>
            <Route path="/authors/*" element={<Routes><Route path="details" element={<Details />} /></Routes>} />
        </Routes>;
        """;

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_not_treat_inner_routes_as_absolute() => Result.Source.Contains("navigate to", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_unknown_mount_once() => Result.Diagnostics.Count(_ => _.Code == "SP0028" && _.Location == RouteFile && _.Message.Contains("unknown parent prefix", StringComparison.Ordinal)).ShouldEqual(1);
}
