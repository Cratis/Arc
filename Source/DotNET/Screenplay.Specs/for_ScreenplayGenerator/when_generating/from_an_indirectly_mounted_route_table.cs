// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_indirectly_mounted_route_table : given.a_screen_with_competing_routes
{
    void Establish()
    {
        Routes = RouteImports + "\nexport const App = () => <Routes><Route path=\"/authors\" element={<Layout />}><Route path=\"overview\" element={<Details />} /></Route><Route path=\"/details\" element={<Details />} /></Routes>;";
        ExtraFiles =
        [
            ("Library/Layout.tsx", "import { AuthorRoutes } from './AuthorRoutes'; export const Layout = () => <AuthorRoutes />;"),
            ("Library/AuthorRoutes.tsx", RouteImports + "\nexport const AuthorRoutes = () => <Routes><Route path=\"other\" element={<Details />} /></Routes>;")
        ];
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_not_let_a_known_route_win_against_an_unknown_mount() => Result.Source.Contains("navigate to", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_indirect_unknown_prefix_once() => Result.Diagnostics.Count(_ => _.Code == "SP0028" && _.Location == "Library/AuthorRoutes.tsx" && _.Message.Contains("unknown parent prefix", StringComparison.Ordinal)).ShouldEqual(1);
}
