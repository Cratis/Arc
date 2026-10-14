// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_own_slice_imports_outside_any_slice_directory : given.a_cross_slice_screen
{
    void Establish()
    {
        Component = """
            import { CommandDialog, DataPage } from '@cratis/components';
            import { RegisterAuthor } from '../../Shared/RegisterAuthor';
            import { RegisteredAuthors } from '../../Shared/RegisteredAuthors';
            export const AuthorBoard = () => <><DataPage query={RegisteredAuthors} /><CommandDialog command={RegisterAuthor} /></>;
            """;
        Files = DeclaredUserInterfaceFiles.Holding(("Library/Authors/Registration/AuthorBoard.tsx", Component));
    }

    void Because() => Generate(Options,
        ("Library/Authors/Listing/Listing.cs", Listing),
        ("Library/Authors/Registration/Registration.cs", Registration));

    [Fact] void should_bind_and_round_trip_the_document() => AssertDocument();
    [Fact] void should_not_bind_the_own_command_by_name_alone() => Result.Model.Slices.SelectMany(_ => _.Screens).Single().Actions.ShouldBeEmpty();
    [Fact] void should_not_bind_the_own_query_by_name_alone() => Result.Model.Slices.SelectMany(_ => _.Screens).Single().Data.ShouldBeEmpty();
    [Fact] void should_not_write_the_bare_action() => Says("action RegisterAuthor").ShouldBeFalse();
    [Fact] void should_not_write_the_bare_query() => Says("data RegisteredAuthor[] via query RegisteredAuthors").ShouldBeFalse();
    [Fact] void should_report_each_unresolved_binding() => Result.Diagnostics.Count(_ => _.Code == "SP0028").ShouldEqual(2);
    [Fact] void should_not_emit_a_table() => Result.Model.Slices.SelectMany(_ => _.Screens).Single().Tables.ShouldBeEmpty();
}
