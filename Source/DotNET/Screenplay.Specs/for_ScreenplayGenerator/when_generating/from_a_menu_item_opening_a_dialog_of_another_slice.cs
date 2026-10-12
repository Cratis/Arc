// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_menu_item_opening_a_dialog_of_another_slice : given.a_screen_using_components
{
    void Establish() => _component = Imports + """

        import { RegisterAuthor } from '../Registration/RegisterAuthor';
        export const AuthorRetirement = () => {
            const [visible, show] = useState(false);
            return <>
                <DataPage query={AuthorsToRetire}><MenuItems><MenuItem command={() => show(true)} /></MenuItems></DataPage>
                <CommandDialog command={RegisterAuthor} visible={visible} />
            </>;
        };
        """;

    void Because() => Generate();

    [Fact] void should_compile_the_document() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_have_no_document_warnings_or_errors() => DocumentIsClean.ShouldBeTrue();
    [Fact] void should_reprint_identically() => _reprinted.ShouldEqual(_result.Source);
    [Fact] void should_not_invent_navigation() => Says("navigate to").ShouldBeFalse();

    [Fact] void should_not_write_a_toolbar() => Says("toolbar").ShouldBeFalse();
    [Fact] void should_report_the_dialog_command_and_menu_item() => Count("SP0028").ShouldEqual(2);
    [Fact] void should_report_the_unread_dialog_command() => Reports("SP0028", "which is not a command its slice declares").ShouldBeTrue();
    [Fact] void should_report_the_unread_opener() => Reports("SP0028", "dialog's command is not read").ShouldBeTrue();
}
