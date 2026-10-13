// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_data_page_whose_menu_items_cannot_be_read : given.a_screen_using_components
{
    void Establish() => _component = Imports + """

        import { useNavigate } from 'react-router-dom';
        export const AuthorRetirement = () => {
            const navigate = useNavigate();
            return <DataPage query={AuthorsToRetire}>
                <MenuItems>
                    <MenuItem label="Named" command={retire} />
                    <MenuItem label="Async" command={async () => await command.execute()} />
                    <MenuItem label="Route" command={() => navigate('/authors')} />
                    <MenuItem {...props} />
                    <MenuItem label="Nothing" />
                    <MenuItem label="Disabled" command={retire} disabled />
                </MenuItems>
                <DataPage.MenuItems>{canEdit && <DataPage.MenuItem command={retire} />}</DataPage.MenuItems>
            </DataPage>;
        };
        """;

    void Because() => Generate();

    [Fact] void should_compile_the_document() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_have_no_document_warnings_or_errors() => DocumentIsClean.ShouldBeTrue();
    [Fact] void should_reprint_identically() => _reprinted.ShouldEqual(_result.Source);
    [Fact] void should_not_invent_navigation() => Says("navigate to").ShouldBeFalse();

    [Fact] void should_not_write_a_toolbar() => Says("toolbar").ShouldBeFalse();
    [Fact] void should_report_each_unread_item_or_menu() => Count("SP0028").ShouldEqual(7);
    [Fact] void should_report_a_named_callback() => Reports("SP0028", "whose command 'retire' is not read").ShouldBeTrue();
    [Fact] void should_report_the_route_without_inventing_a_screen() => Reports("SP0028", "navigates to route '/authors'").ShouldBeTrue();
    [Fact] void should_report_the_conditional_menu() => Reports("SP0028", "conditional or mixed children").ShouldBeTrue();
    [Fact] void should_report_a_missing_command() => Reports("SP0028", "does nothing when clicked").ShouldBeTrue();
    [Fact] void should_report_a_disabled_item() => Reports("SP0028", "disabled").ShouldBeTrue();
}
