// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_menu_item_opening_a_dialog_of_another_slice : given.a_cross_slice_screen
{
    void Establish() => Component = """
        import { DataPage, MenuItem, MenuItems, CommandDialog } from '@cratis/components';
        import { useState } from 'react';
        import { All } from './All';
        import { RegisterAuthor } from '../Registration/RegisterAuthor';
        export const AuthorBoard = () => {
            const [visible, show] = useState(false);
            return <>
                <DataPage query={All}><MenuItems><MenuItem command={() => show(true)} /></MenuItems></DataPage>
                <CommandDialog command={RegisterAuthor} visible={visible} />
            </>;
        };
        """;

    void Because() => GenerateScreen();

    [Fact] void should_bind_and_round_trip_the_document() => AssertDocument();
    [Fact] void should_write_the_qualified_toolbar_item() => Says("item registerAuthor action Registration.RegisterAuthor for screen").ShouldBeTrue();
    [Fact] void should_not_write_the_claimed_dialog_as_a_screen_action() => Says("\n                action ").ShouldBeFalse();
    [Fact] void should_not_report_the_read_command() => Result.Diagnostics.Where(_ => _.Code == "SP0028").ShouldBeEmpty();
    [Fact] void should_not_infer_or_warn_about_the_action_subject() => Bound.Diagnostics.Where(_ => _.Code == "PLAY0818" || _.Code == "PLAY0819").ShouldBeEmpty();
    [Fact] void should_not_invent_navigation() => Says("navigate to").ShouldBeFalse();
}
