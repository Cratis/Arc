// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_own_slice_imports_from_unresolvable_modules : given.a_cross_slice_screen
{
    void Establish()
    {
        Component = """
            import { CommandDialog, DataPage } from '@cratis/components';
            import { RegisterAuthor, RegisteredAuthors } from '../../../../../../Proxies';
            export const AuthorBoard = () => <><DataPage query={RegisteredAuthors} /><CommandDialog command={RegisterAuthor} /></>;
            """;
        Files = DeclaredUserInterfaceFiles.Holding(("Library/Authors/Registration/AuthorBoard.tsx", Component));
    }

    void Because() => Generate(Options,
        ("Library/Authors/Listing/Listing.cs", Listing),
        ("Library/Authors/Registration/Registration.cs", Registration));

    [Fact] void should_bind_and_round_trip_the_document() => AssertDocument();
    [Fact] void should_report_each_unresolved_binding() => Result.Diagnostics.Count(_ => _.Code == "SP0028").ShouldEqual(2);
    [Fact] void should_not_emit_any_action_or_data_binding() => Result.Model.Slices.SelectMany(_ => _.Screens).Single().Actions.Concat(Result.Model.Slices.SelectMany(_ => _.Screens).Single().Data.Select(_ => _.Query)).ShouldBeEmpty();
    [Fact] void should_not_emit_a_table() => Result.Model.Slices.SelectMany(_ => _.Screens).Single().Tables.ShouldBeEmpty();
}
