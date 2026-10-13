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
    [Fact] void should_keep_the_own_command_bare() => Result.Model.Slices.SelectMany(_ => _.Screens).Single().Actions.ShouldContainOnly(["RegisterAuthor"]);
    [Fact] void should_keep_the_own_query_bare() => Result.Model.Slices.SelectMany(_ => _.Screens).Single().Data.Select(_ => _.Query).ShouldContainOnly(["RegisteredAuthors"]);
    [Fact] void should_write_the_bare_action() => Says("action RegisterAuthor").ShouldBeTrue();
    [Fact] void should_write_the_bare_query() => Says("data RegisteredAuthor[] via query RegisteredAuthors").ShouldBeTrue();
    [Fact] void should_not_report_an_unresolved_binding() => Result.Diagnostics.Where(_ => _.Code == "SP0028").ShouldBeEmpty();
}
