// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_data_page_with_an_orphan_menu_item : given.a_screen_using_components
{
    void Establish() => _component = Imports + """

        export const AuthorRetirement = () => {
            const [visible, show] = useState(false);
            return <>
                <DataPage query={AuthorsToRetire}><MenuItem command={() => show(true)} /></DataPage>
                <CommandDialog command={RetireAuthor} visible={visible} />
            </>;
        };
        """;

    void Because() => Generate();

    [Fact] void should_not_write_a_toolbar() => Says("toolbar").ShouldBeFalse();
    [Fact] void should_keep_the_dialog_action() => Says("\n        action RetireAuthor").ShouldBeTrue();
    [Fact] void should_report_the_nonrendering_marker() => Reports("SP0028", "outside a MenuItems container, which renders nothing").ShouldBeTrue();
    [Fact] void should_report_only_the_orphan() => Count("SP0028").ShouldEqual(1);
}
