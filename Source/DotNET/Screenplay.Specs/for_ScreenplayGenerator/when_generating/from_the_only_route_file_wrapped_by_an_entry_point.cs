// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_the_only_route_file_wrapped_by_an_entry_point : given.a_screen_with_competing_routes
{
    void Establish()
    {
        Routes = RouteImports + "\nexport const App = () => <Routes><Route path=\"/details\" element={<Details />} /></Routes>;";
        ExtraFiles = [("Library/main.tsx", """
            import { BrowserRouter } from 'react-router-dom';
            import { App } from './App';
            export const Main = () => <BrowserRouter><App /></BrowserRouter>;
            """)];
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_treat_the_only_route_declaring_file_as_a_root() => Result.Source.ShouldContain("item authorDetails navigate to Details.AuthorDetails");
    [Fact] void should_not_count_a_router_only_entry_point_as_a_route_table() => Result.Diagnostics.Any(_ => _.Code == "SP0028" && _.Message.Contains("unknown parent prefix", StringComparison.Ordinal)).ShouldBeFalse();
}
