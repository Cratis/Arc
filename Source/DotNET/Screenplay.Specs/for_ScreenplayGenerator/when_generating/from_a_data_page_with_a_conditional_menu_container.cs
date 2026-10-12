// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_data_page_with_a_conditional_menu_container : given.a_screen_using_components
{
    void Establish() => _component = Imports + """

        export const AuthorRetirement = () => {
            const [visible, show] = useState(false);
            return <>
                <DataPage query={AuthorsToRetire}>
                    {canRetire && <MenuItems><MenuItem command={() => show(true)} /></MenuItems>}
                </DataPage>
                <CommandDialog command={RetireAuthor} visible={visible} />
            </>;
        };
        """;

    void Because() => Generate();

    [Fact] void should_not_state_an_unconditional_toolbar() => Says("toolbar").ShouldBeFalse();
    [Fact] void should_keep_the_unclaimed_dialog_action() => Says("\n        action RetireAuthor").ShouldBeTrue();
    [Fact] void should_report_the_conditional_container() => Reports("SP0028", "conditional menu container").ShouldBeTrue();
    [Fact] void should_report_only_the_container() => Count("SP0028").ShouldEqual(1);
}
