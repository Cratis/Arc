// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_unreadable_router_declarations : given.a_screen_with_routes
{
    void Establish()
    {
        Routes = RouteImports + """

            import { Navigate } from 'react-router-dom';
            const Lazy = lazy(() => import('./Authors/Details/AuthorDetails'));
            export const App = () => <Routes>
                <Route path="/authors" element={<Details />} />
                <Route path="/authors" element={<Details />} />
                <Route path="/lazy" element={<Lazy />} />
                <Route path={destination} element={<Details />} />
                <Route path="/computed" element={renderDetails()} />
                <Route path="/redirect" element={<Navigate to="/authors" />} />
                <Route path="*" element={<Details />} />
            </Routes>;
            """;
        Component = Imports + """

            export const AuthorRetirement = () => {
                const navigate = useNavigate();
                return <DataPage title="Authors"><DataPage.MenuItems>
                    <DataPage.MenuItem command={() => navigate('/authors')} />
                    <DataPage.MenuItem command={() => navigate('/lazy')} />
                    <DataPage.MenuItem command={() => navigate('/computed')} />
                    <DataPage.MenuItem command={() => navigate('/redirect')} />
                </DataPage.MenuItems></DataPage>;
            };
            """;
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_not_guess_any_destination() => Result.Source.Contains("navigate to", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_once_for_the_route_table() => Result.Diagnostics.Count(_ => _.Code == "SP0028" && _.Location == RouteFile).ShouldEqual(1);
    [Fact] void should_name_the_duplicate_path() => Result.Diagnostics.Single(_ => _.Location == RouteFile).Message.ShouldContain("duplicate path '/authors'");
    [Fact] void should_list_the_unread_lazy_element() => Result.Diagnostics.Single(_ => _.Location == RouteFile).Message.ShouldContain("<Lazy />");
    [Fact] void should_list_computed_paths_and_wildcards() => Result.Diagnostics.Single(_ => _.Location == RouteFile).Message.ShouldContain("unsupported path/index");
    [Fact] void should_report_each_unmapped_menu_gesture() => Result.Diagnostics.Count(_ => _.Code == "SP0028" && _.Message.Contains("navigates to route", StringComparison.Ordinal)).ShouldEqual(4);
}
