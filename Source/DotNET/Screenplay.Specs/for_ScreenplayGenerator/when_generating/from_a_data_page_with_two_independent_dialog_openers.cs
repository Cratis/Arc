// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_data_page_with_two_independent_dialog_openers : given.a_screen_using_components
{
    void Establish() => _component = Imports + """

        export const AuthorRetirement = () => {
            const [addVisible, add] = useState(false);
            const [editVisible, edit] = useState(false);
            return <>
                <DataPage query={AuthorsToRetire}><MenuItems>
                    <MenuItem label="Add" command={() => add(true)} />
                    <MenuItem label="Edit" command={() => edit(true)} />
                </MenuItems></DataPage>
                <CommandDialog command={RetireAuthor} visible={addVisible} />
                <CommandDialog command={RetireAuthor} visible={editVisible} />
            </>;
        };
        """;

    void Because() => Generate();

    [Fact] void should_state_the_add_opener() => Says("item retireAuthor action RetireAuthor for screen\n            label \"Add\"").ShouldBeTrue();
    [Fact] void should_state_the_edit_opener() => Says("item retireAuthor2 action RetireAuthor for screen\n            label \"Edit\"").ShouldBeTrue();
    [Fact] void should_claim_both_dialogs() => Says("\n        action RetireAuthor").ShouldBeFalse();
    [Fact] void should_not_report_either_opener_as_unread() => Count("SP0028").ShouldEqual(0);
    [Fact] void should_compile_the_document() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_reprint_identically() => _reprinted.ShouldEqual(_result.Source);
}
