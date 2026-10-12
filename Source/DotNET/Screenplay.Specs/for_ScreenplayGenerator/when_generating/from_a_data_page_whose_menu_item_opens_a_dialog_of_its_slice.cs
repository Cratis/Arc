// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_data_page_whose_menu_item_opens_a_dialog_of_its_slice : given.a_screen_using_components
{
    void Establish() => _component = Imports + """

        export const AuthorRetirement = () => {
            const [visible, show] = useState<boolean>(false);
            return <>
                <DataPage title="Retire authors" query={AuthorsToRetire}>
                    <DataPage.MenuItems>
                        <DataPage.MenuItem label="Retire" command={() => show(true)} disableOnUnselected />
                    </DataPage.MenuItems>
                    <DataPage.Columns><Column field="name" header="Name" /></DataPage.Columns>
                </DataPage>
                <CommandDialog command={RetireAuthor} visible={visible} />
            </>;
        };
        """;

    void Because() => Generate();

    [Fact] void should_compile_the_application_source() => Analyzed.ErrorsIn(("Library/Authors/Retirement/Retirement.cs", LibrarySource.AuthorRetirement)).ShouldBeEmpty();
    [Fact] void should_compile_the_document() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_have_no_document_warnings_or_errors() => DocumentIsClean.ShouldBeTrue();
    [Fact] void should_reprint_identically() => _reprinted.ShouldEqual(_result.Source);
    [Fact] void should_not_invent_navigation() => Says("navigate to").ShouldBeFalse();

    [Fact] void should_write_the_toolbar_item_and_label() => Says("toolbar actions\n          item retireAuthor action RetireAuthor\n            label \"Retire\"").ShouldBeTrue();
    [Fact] void should_keep_the_title_and_table() => Says("table Author\n          column name label \"Name\"").ShouldBeTrue();
    [Fact] void should_not_repeat_the_claimed_dialog_as_a_screen_action() => Says("\n        action RetireAuthor").ShouldBeFalse();
    [Fact] void should_report_selection_enablement() => Reports("SP0077", "enablement is not stated").ShouldBeTrue();
    [Fact] void should_report_only_selection_enablement_as_unrepresentable() => Count("SP0077").ShouldEqual(1);
    [Fact] void should_read_all_the_menu_behavior() => Count("SP0028").ShouldEqual(0);
}
