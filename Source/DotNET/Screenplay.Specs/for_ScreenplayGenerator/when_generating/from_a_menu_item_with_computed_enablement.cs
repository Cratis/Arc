// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_menu_item_with_computed_enablement : given.a_screen_using_components
{
    void Establish() => _component = Imports + """

        export const AuthorRetirement = () => {
            const [visible, show] = useState(false);
            return <>
                <DataPage query={AuthorsToRetire}><MenuItems>
                    <MenuItem label="Retire" command={() => show(true)} disabled={!canRetire} />
                </MenuItems></DataPage>
                <CommandDialog command={RetireAuthor} visible={visible} />
            </>;
        };
        """;

    void Because() => Generate();

    [Fact] void should_not_write_a_toolbar() => Says("toolbar").ShouldBeFalse();
    [Fact] void should_keep_the_dialog_action() => Says("\n        action RetireAuthor").ShouldBeTrue();
    [Fact] void should_report_that_the_item_is_omitted() => Reports("SP0077", "no toolbar item is written and the command stays a screen action").ShouldBeTrue();
    [Fact] void should_report_only_the_enablement_gap() => Count("SP0077").ShouldEqual(1);
    [Fact] void should_not_report_an_unread_callback() => Count("SP0028").ShouldEqual(0);
}
