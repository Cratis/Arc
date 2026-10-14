// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_shadowed_navigation_hook : given.a_screen_with_routes
{
    void Establish()
    {
        Routes = RouteImports + "\nexport const App = () => <Routes><Route path=\"/authors\" element={<Details />} /></Routes>;";
        Component = Imports + """

            export const AuthorRetirement = (useNavigate) => {
                const navigate = useNavigate();
                return <DataPage title="Authors"><DataPage.MenuItems><DataPage.MenuItem command={() => navigate('/authors')} /></DataPage.MenuItems></DataPage>;
            };
            """;
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_not_treat_a_parameter_as_the_imported_router_hook() => Result.Source.Contains("navigate to", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_unproven_gesture() => Result.Diagnostics.Any(_ => _.Code == "SP0028" && _.Message.Contains("navigates to route", StringComparison.Ordinal)).ShouldBeTrue();
}
