// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

public class a_screen_with_competing_routes : a_screen_with_routes
{
    void Establish() => Component = Imports + """

        export const AuthorRetirement = () => {
            const navigate = useNavigate();
            return <DataPage title="Authors"><DataPage.MenuItems><DataPage.MenuItem command={() => navigate('/details')} /></DataPage.MenuItems></DataPage>;
        };
        """;
}
