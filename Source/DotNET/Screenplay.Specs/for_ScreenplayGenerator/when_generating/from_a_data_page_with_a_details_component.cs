// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_data_page_with_a_details_component : given.a_screen_using_components
{
    void Establish() => _component = Imports + """

        import { AuthorDetails } from './AuthorDetails';
        export const AuthorRetirement = () => <DataPage query={AuthorsToRetire} detailsComponent={AuthorDetails} />;
        """;

    void Because() => Generate();

    [Fact] void should_compile_the_document() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_have_no_document_warnings_or_errors() => DocumentIsClean.ShouldBeTrue();
    [Fact] void should_reprint_identically() => _reprinted.ShouldEqual(_result.Source);
    [Fact] void should_not_invent_navigation() => Says("navigate to").ShouldBeFalse();

    [Fact] void should_not_invent_composition() => Says("component AuthorDetails").ShouldBeFalse();
    [Fact] void should_not_invent_a_section() => Says("section").ShouldBeFalse();
    [Fact] void should_report_same_screen_composition() => Reports("SP0077", "same-screen composition").ShouldBeTrue();
    [Fact] void should_report_only_the_composition_gap() => Count("SP0077").ShouldEqual(1);
}
