// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_router_object_in_a_frontend_folder : given.a_screen_with_routes
{
    void Establish()
    {
        RouteFile = "Library/.frontend/routing/App.tsx";
        Routes = """
            import { createBrowserRouter as router } from 'react-router';
            import { AuthorDetails as Details } from '../../Authors/Details/AuthorDetails.tsx';
            import { Layout } from '../../Layout';
            export const routes = router([
                { element: <Layout />, children: [
                    { path: '/authors', children: [{ index: true, element: <Details /> }] }
                ] }
            ]);
            """;
        Component = Imports + """

            export const AuthorRetirement = () => {
                const navigate = useNavigate();
                return <DataPage title="Authors"><DataPage.MenuItems><DataPage.MenuItem command={() => navigate('/authors')} /></DataPage.MenuItems></DataPage>;
            };
            """;
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_find_the_nested_frontend_route_table() => Result.Source.ShouldContain("navigate to Details.AuthorDetails");
    [Fact] void should_read_layout_and_index_literals_without_a_table_omission() => Result.Diagnostics.Any(_ => _.Code == "SP0028" && _.Location == RouteFile).ShouldBeFalse();
}
