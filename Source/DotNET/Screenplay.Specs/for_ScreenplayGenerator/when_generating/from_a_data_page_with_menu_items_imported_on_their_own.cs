// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_data_page_with_menu_items_imported_on_their_own : given.a_screen_using_components
{
    void Establish() => _component = Imports + """

        export const AuthorRetirement = () => {
            const [visible, show] = useState(false);
            return <>
                <DataPage query={AuthorsToRetire}>
                    <MenuItems>
                        <MenuItem label="Retire" command={() => { show(true); }} />
                        <MenuItem label="Opaque" command={retire} />
                    </MenuItems>
                </DataPage>
                <CommandDialog command={RetireAuthor} visible={visible} />
            </>;
        };
        """;

    void Because() => Generate();

    [Fact] void should_compile_the_document() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_have_no_document_warnings_or_errors() => DocumentIsClean.ShouldBeTrue();
    [Fact] void should_reprint_identically() => _reprinted.ShouldEqual(_result.Source);
    [Fact] void should_not_invent_navigation() => Says("navigate to").ShouldBeFalse();

    [Fact] void should_write_the_standalone_item() => Says("item retireAuthor action RetireAuthor").ShouldBeTrue();
    [Fact] void should_not_silently_drop_the_callback() => Reports("SP0028", "whose command 'retire' is not read").ShouldBeTrue();
    [Fact] void should_report_only_the_callback() => Count("SP0028").ShouldEqual(1);
}
