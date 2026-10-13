// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_menu_item_opening_a_screen_through_use_dialog : given.a_screen_using_components
{
    void Establish() => _component = Imports + """

        import { useDialog } from '@cratis/arc.react/dialogs';
        import { AuthorDetails } from './AuthorDetails';
        export const AuthorRetirement = () => {
            const [Dialog, show] = useDialog(AuthorDetails);
            return <DataPage query={AuthorsToRetire}>
                <MenuItems><MenuItem command={() => show()} /></MenuItems>
            </DataPage>;
        };
        """;

    void Because() => Generate();

    [Fact] void should_compile_the_document() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_have_no_document_warnings_or_errors() => DocumentIsClean.ShouldBeTrue();
    [Fact] void should_reprint_identically() => _reprinted.ShouldEqual(_result.Source);
    [Fact] void should_not_invent_navigation() => Says("navigate to").ShouldBeFalse();

    [Fact] void should_not_write_a_toolbar() => Says("toolbar").ShouldBeFalse();
    [Fact] void should_report_the_screen_opened_as_a_dialog() => Reports("SP0077", "opens AuthorDetails as a dialog").ShouldBeTrue();
    [Fact] void should_report_only_the_composition_gap() => Count("SP0077").ShouldEqual(1);
}
