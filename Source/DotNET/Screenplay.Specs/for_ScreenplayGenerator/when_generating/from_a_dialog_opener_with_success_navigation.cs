// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_dialog_opener_with_success_navigation : given.a_screen_with_routes
{
    void Establish()
    {
        Routes = RouteImports + "\nexport const App = () => <Routes><Route path=\"/authors\" element={<Details />} /></Routes>;";
        Component = Imports + """

            import { useState } from 'react';
            export const AuthorRetirement = () => {
                const navigate = useNavigate();
                const [visible, show] = useState(false);
                return <DataPage title="Authors"><DataPage.MenuItems>
                    <DataPage.MenuItem command={() => show(true)} />
                </DataPage.MenuItems><CommandDialog command={RetireAuthor} visible={visible} onSuccess={() => navigate('/authors')} /></DataPage>;
            };
            """;
    }

    void Because() => GenerateNavigation();

    [Fact] void should_round_trip_and_bind() => AssertDocument();
    [Fact] void should_keep_the_item_as_a_command_input_opener() => Result.Source.ShouldContain("item retireAuthor action RetireAuthor");
    [Fact] void should_attach_navigation_to_the_separate_command_action() => Result.Source.ShouldContain("action RetireAuthor for screen\n");
    [Fact] void should_preserve_the_success_route() => Result.Source.ShouldContain("route \"/authors\"");
}
