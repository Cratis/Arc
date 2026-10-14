// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_navigation_without_a_route_table : given.a_screen_with_routes
{
    void Establish()
    {
        Routes = string.Empty;
        Component = Imports + """

            export const AuthorRetirement = () => {
                const navigate = useNavigate();
                return <DataPage title="Authors"><DataPage.MenuItems><DataPage.MenuItem command={() => navigate('/authors')} /></DataPage.MenuItems><CommandDialog command={RetireAuthor} onSuccess={() => navigate('/authors')} /></DataPage>;
            };
            """;
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_not_invent_navigation() => Result.Source.Contains("navigate to", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_preserve_the_unread_route_report() => Result.Diagnostics.Any(_ => _.Code == "SP0028" && _.Message.Contains("navigates to route '/authors'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_preserve_the_unread_success_report() => Result.Diagnostics.Any(_ => _.Code == "SP0028" && _.Message.Contains("after success", StringComparison.Ordinal)).ShouldBeTrue();
}
