// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_navigation_to_the_screens_own_slice : given.a_screen_with_routes
{
    void Establish()
    {
        Routes = """
            import { Route, Routes } from 'react-router-dom';
            import { AuthorRetirement } from './Authors/Retirement/AuthorRetirement';
            export const App = () => <Routes><Route path="/authors" element={<AuthorRetirement />} /></Routes>;
            """;
        Component = Imports + """

            export const AuthorRetirement = () => {
                const navigate = useNavigate();
                return <DataPage title="Authors"><DataPage.MenuItems><DataPage.MenuItem command={() => navigate('/authors')} /></DataPage.MenuItems></DataPage>;
            };
            """;
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_keep_an_own_slice_destination_bare() => Result.Source.ShouldContain("item authorRetirement navigate to AuthorRetirement");
}
