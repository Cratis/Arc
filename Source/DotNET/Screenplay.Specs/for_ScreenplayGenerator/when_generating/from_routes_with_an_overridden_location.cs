// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_routes_with_an_overridden_location : given.a_screen_with_routes
{
    void Establish()
    {
        Routes = RouteImports + "\nexport const App = () => <Routes location=\"/elsewhere\"><Route path=\"/authors\" element={<Details />} /></Routes>;";
        Component = Imports + """

            export const AuthorRetirement = () => {
                const navigate = useNavigate();
                return <CommandDialog command={RetireAuthor} onSuccess={() => navigate('/authors')} />;
            };
            """;
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_not_assume_navigation_changes_a_table_with_a_fixed_location() => Result.Source.Contains("navigate to", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_unread_table_once() => Result.Diagnostics.Count(_ => _.Code == "SP0028" && _.Location == RouteFile).ShouldEqual(1);
}
