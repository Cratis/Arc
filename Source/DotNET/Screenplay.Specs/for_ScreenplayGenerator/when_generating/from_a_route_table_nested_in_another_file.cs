// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_route_table_nested_in_another_file : given.a_screen_with_routes
{
    void Establish()
    {
        Routes = """
            import { Route, Routes } from 'react-router-dom';
            import { AuthorRoutes } from './AuthorRoutes';
            export const App = () => <Routes><Route path="/authors/*" element={<AuthorRoutes />} /></Routes>;
            """;
        ExtraFiles = [("Library/AuthorRoutes.tsx", RouteImports + "\nexport const AuthorRoutes = () => <Routes><Route path=\"details\" element={<Details />} /></Routes>;")];
        Component = Imports + """

            export const AuthorRetirement = () => {
                const navigate = useNavigate();
                return <DataPage title="Authors"><DataPage.MenuItems><DataPage.MenuItem command={() => navigate('/details')} /></DataPage.MenuItems></DataPage>;
            };
            """;
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_not_treat_a_nested_table_as_a_root() => Result.Source.Contains("navigate to", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_unknown_parent_prefix_once() => Result.Diagnostics.Count(_ => _.Code == "SP0028" && _.Location == "Library/AuthorRoutes.tsx" && _.Message.Contains("unknown parent prefix", StringComparison.Ordinal)).ShouldEqual(1);
}
