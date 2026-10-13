// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_menu_item_with_ambiguous_setter_tuples : given.a_screen_using_components
{
    void Establish() => _component = Imports + """

        function Other() { const [otherVisible, show] = useState(false); }
        export const AuthorRetirement = () => {
            const [visible, show] = useState(false);
            return <>
                <DataPage query={AuthorsToRetire}><MenuItems><MenuItem command={() => show(true)} /></MenuItems></DataPage>
                <CommandDialog command={RetireAuthor} visible={visible} />
            </>;
        };
        """;

    void Because() => Generate();

    [Fact] void should_not_write_a_toolbar() => Says("toolbar").ShouldBeFalse();
    [Fact] void should_keep_the_unclaimed_dialog_action() => Says("\n        action RetireAuthor").ShouldBeTrue();
    [Fact] void should_report_only_the_unread_callback() => Count("SP0028").ShouldEqual(1);
}
