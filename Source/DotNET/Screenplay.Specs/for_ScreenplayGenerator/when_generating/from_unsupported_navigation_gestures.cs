// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_unsupported_navigation_gestures : given.a_screen_with_routes
{
    void Establish()
    {
        Routes = RouteImports + "\nexport const App = () => <Routes><Route path=\"/authors\" element={<Details />} /></Routes>;";
        Component = Imports + """

            import { Link } from 'react-router-dom';
            export const AuthorRetirement = () => {
                const navigate = useNavigate();
                return <DataPage title="Authors" onRowClick={() => navigate('/authors')}>
                    <DataPage.MenuItems>
                        <DataPage.MenuItem command={() => navigate('..')} />
                        <DataPage.MenuItem command={() => navigate(`/authors`)} />
                        <DataPage.MenuItem command={() => navigate(destination)} />
                        <DataPage.MenuItem command={() => navigate('/authors', { state: { id: 42 } })} />
                    </DataPage.MenuItems>
                    <Link to="/authors">Authors</Link>
                    <button onClick={() => navigate('/authors')}>Authors</button>
                </DataPage>;
            };
            """;
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_not_map_unsupported_gestures() => Result.Source.Contains("navigate to", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_all_unread_menu_callbacks() => Result.Diagnostics.Count(_ => _.Code == "SP0028" && _.Message.Contains("menu item", StringComparison.Ordinal)).ShouldEqual(4);
    [Fact] void should_report_link_navigation_inside_a_known_component() => Result.Diagnostics.Any(_ => _.Code == "SP0028" && _.Message.Contains("<Link> to target", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_inline_click_navigation_inside_a_known_component() => Result.Diagnostics.Any(_ => _.Code == "SP0028" && _.Message.Contains("onClick navigation", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_row_click_navigation() => Result.Diagnostics.Any(_ => _.Code == "SP0028" && _.Message.Contains("onRowClick navigation", StringComparison.Ordinal)).ShouldBeTrue();
}
