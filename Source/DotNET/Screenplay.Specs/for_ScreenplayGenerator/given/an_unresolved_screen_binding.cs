// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

public class an_unresolved_screen_binding : a_cross_slice_screen
{
    protected string Module;
    protected string Command = "RegisterAuthor";
    protected string Query = "All";

    protected void GenerateUnresolvedScreen()
    {
        Component = $$"""
            import { CommandDialog, DataPage } from '@cratis/components';
            import { {{Command}} as MissingCommand, {{Query}} as MissingQuery } from '{{Module}}';
            export const AuthorBoard = () => <><DataPage query={MissingQuery} /><CommandDialog command={MissingCommand} /></>;
            """;
        GenerateScreen();
    }
}
