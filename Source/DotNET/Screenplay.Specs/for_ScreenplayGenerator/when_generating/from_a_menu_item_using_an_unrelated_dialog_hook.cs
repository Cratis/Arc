// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_menu_item_using_an_unrelated_dialog_hook : given.a_screen_using_components
{
    void Establish() => _component = Imports + """

        import { useDialog } from 'other-dialogs';
        import { AuthorDetails } from './AuthorDetails';
        export const AuthorRetirement = () => {
            const [Dialog, show] = useDialog(AuthorDetails);
            return <DataPage query={AuthorsToRetire}>
                <MenuItems><MenuItem command={() => show()} /></MenuItems>
            </DataPage>;
        };
        """;

    void Because() => Generate();

    [Fact] void should_not_write_a_toolbar() => Says("toolbar").ShouldBeFalse();
    [Fact] void should_not_claim_to_know_the_dialog_hook() => Count("SP0077").ShouldEqual(0);
    [Fact] void should_report_the_unread_callback() => Count("SP0028").ShouldEqual(1);
}
