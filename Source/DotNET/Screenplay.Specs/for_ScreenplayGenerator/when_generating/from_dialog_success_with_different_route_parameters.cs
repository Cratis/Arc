// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_dialog_success_with_different_route_parameters : given.a_screen_with_routes
{
    void Establish()
    {
        Routes = RouteImports + "\nexport const App = () => <Routes><Route path=\"/authors/:id\" element={<Details />} /></Routes>;";
        Component = Imports + """

            export const AuthorRetirement = () => {
                const navigate = useNavigate();
                return <>
                    <CommandDialog command={RetireAuthor} onSuccess={() => navigate('/authors/1')} />
                    <CommandDialog command={RetireAuthor} onSuccess={() => navigate('/authors/2')} />
                </>;
            };
            """;
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_not_merge_destinations_with_different_literal_parameters() => Result.Source.Contains("navigate to", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_report_the_disagreement() => Result.Diagnostics.Count(_ => _.Code == "SP0028" && _.Message.Contains("multiple dialogs with different post-success behavior", StringComparison.Ordinal)).ShouldEqual(1);
}
