// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_menu_navigating_to_a_parameterized_route : given.a_screen_with_routes
{
    void Establish()
    {
        Routes = RouteImports + "\nexport const App = () => <Routes><Route path=\"/authors/:id\" element={<Details />} /></Routes>;";
        Component = Imports + """

            export const AuthorRetirement = () => {
                const navigate = useNavigate();
                return <DataPage title="Authors"><DataPage.MenuItems><DataPage.MenuItem command={() => navigate('/authors/42')} /></DataPage.MenuItems></DataPage>;
            };
            """;
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_emit_a_navigation_item() => Result.Source.ShouldContain("item authorDetails navigate to Details.AuthorDetails");
    [Fact] void should_bind_the_parameter() => Result.Source.ShouldContain("parameter id from literal \"42\"");
    [Fact] void should_not_emit_a_route_option_the_toolbar_grammar_lacks() => Result.Source.Contains("route \"/authors/42\"", StringComparison.Ordinal).ShouldBeFalse();
}
