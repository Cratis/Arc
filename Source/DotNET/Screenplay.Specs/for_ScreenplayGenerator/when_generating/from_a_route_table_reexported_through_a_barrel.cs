// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_route_table_reexported_through_a_barrel : given.a_screen_with_competing_routes
{
    void Establish()
    {
        Routes = RouteImports + "\nexport const App = () => <Routes><Route path=\"/authors\" element={<Layout />}><Route path=\"overview\" element={<Details />} /></Route></Routes>;";
        ExtraFiles =
        [
            ("Library/Layout.tsx", "import AuthorRoutes from './routing'; export const Layout = () => <AuthorRoutes />;"),
            ("Library/routing/index.ts", "export { AuthorRoutes as default } from './AuthorRoutes';"),
            ("Library/routing/AuthorRoutes.tsx", """
                import { Route, Routes } from 'react-router-dom';
                import { AuthorDetails as Details } from '../Authors/Details/AuthorDetails';
                import { Layout } from '../Layout';
                export const AuthorRoutes = () => <Routes><Route path="details" element={<Details />} /></Routes>;
                """)
        ];
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_not_treat_a_reexported_nested_table_as_a_root() => Result.Source.Contains("navigate to", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_unknown_prefix_without_reading_the_barrel() => Result.Diagnostics.Count(_ => _.Code == "SP0028" && _.Location == "Library/routing/AuthorRoutes.tsx" && _.Message.Contains("unknown parent prefix", StringComparison.Ordinal)).ShouldEqual(1);
}
