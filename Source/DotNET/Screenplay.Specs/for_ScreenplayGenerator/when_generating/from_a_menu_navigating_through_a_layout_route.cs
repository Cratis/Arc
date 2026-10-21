// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_menu_navigating_through_a_layout_route : given.a_screen_with_routes
{
    void Establish()
    {
        Routes = RouteImports + """

            export const App = () => <Routes><Route element={<Layout />}><Route path="/authors" element={<Details />} /></Route></Routes>;
            """;
        Component = Imports + """

            export const AuthorRetirement = () => {
                const navigate = useNavigate();
                return <DataPage title="Authors"><DataPage.MenuItems><DataPage.MenuItem label="Details" command={() => navigate('/authors')} /></DataPage.MenuItems></DataPage>;
            };
            """;
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_emit_a_qualified_navigation_item() => Result.Source.ShouldContain("item authorDetails navigate to Details.AuthorDetails");
    [Fact] void should_preserve_the_label() => Result.Source.ShouldContain("label \"Details\"");
    [Fact] void should_not_report_the_resolved_menu_route() => Result.Diagnostics.Any(_ => _.Code == "SP0028" && _.Message.Contains("menu item", StringComparison.Ordinal)).ShouldBeFalse();
}
