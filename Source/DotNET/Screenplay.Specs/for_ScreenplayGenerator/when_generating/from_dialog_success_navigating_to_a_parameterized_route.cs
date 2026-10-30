// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_dialog_success_navigating_to_a_parameterized_route : given.a_screen_with_routes
{
    void Establish()
    {
        Routes = RouteImports + """

            export const App = () => <Routes><Route path="authors"><Route path=":id" element={<Details />} /></Route></Routes>;
            """;
        Component = Imports + """

            export const AuthorRetirement = () => {
                const navigate = useNavigate();
                return <CommandDialog command={RetireAuthor} onSuccess={() => navigate('/authors/42')} />;
            };
            """;
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_emit_navigation_after_the_action() => Result.Source.ShouldContain("action RetireAuthor for screen\n");
    [Fact] void should_qualify_the_destination() => Result.Source.ShouldContain("navigate to Details.AuthorDetails");
    [Fact] void should_preserve_the_route_table_pattern() => Result.Source.ShouldContain("route \"/authors/:id\"");
    [Fact] void should_not_replace_the_pattern_with_the_gesture_destination() => Result.Source.Contains("route \"/authors/42\"", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_bind_the_literal_parameter() => Result.Source.ShouldContain("parameter id from literal \"42\"");
    [Fact] void should_not_report_read_success_navigation() => Result.Diagnostics.Any(_ => _.Code == "SP0028" && _.Message.Contains("after success", StringComparison.Ordinal)).ShouldBeFalse();
}
