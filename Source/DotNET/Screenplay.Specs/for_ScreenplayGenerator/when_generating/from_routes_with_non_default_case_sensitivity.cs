// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_routes_with_non_default_case_sensitivity : given.a_screen_with_routes
{
    void Establish()
    {
        Routes = RouteImports + "\nexport const App = () => <Routes><Route path=\"/AUTHORS\" caseSensitive element={<Details />} /></Routes>;";
        Component = Imports + """

            export const AuthorRetirement = () => {
                const navigate = useNavigate();
                return <CommandDialog command={RetireAuthor} onSuccess={() => navigate('/authors')} />;
            };
            """;
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_not_assume_case_insensitive_matching_for_a_sensitive_route() => Result.Source.Contains("navigate to", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_unread_sensitivity_once() => Result.Diagnostics.Count(_ => _.Code == "SP0028" && _.Location == RouteFile && _.Message.Contains("caseSensitive", StringComparison.Ordinal)).ShouldEqual(1);
}
