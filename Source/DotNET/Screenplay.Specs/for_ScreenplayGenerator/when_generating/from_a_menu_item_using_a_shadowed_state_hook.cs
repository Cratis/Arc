// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_menu_item_using_a_shadowed_state_hook : given.a_screen_using_components
{
    void Establish() => _component = Imports + """

        export const AuthorRetirement = () => {
            const { useState } = otherState;
            const [visible, show] = useState(false);
            return <>
                <DataPage query={AuthorsToRetire}><MenuItems><MenuItem command={() => show(true)} /></MenuItems></DataPage>
                <CommandDialog command={RetireAuthor} visible={visible} />
            </>;
        };
        """;

    void Because() => Generate();

    [Fact] void should_not_confuse_the_local_hook_with_react() => Says("toolbar").ShouldBeFalse();
    [Fact] void should_keep_the_unclaimed_dialog_action() => Says("\n        action RetireAuthor").ShouldBeTrue();
    [Fact] void should_report_only_the_unread_callback() => Count("SP0028").ShouldEqual(1);
}
