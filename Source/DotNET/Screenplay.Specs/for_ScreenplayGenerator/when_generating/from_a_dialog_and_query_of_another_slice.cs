// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_dialog_and_query_of_another_slice : given.a_generated_document_without_executable_binding
{
    const string Component = """
        import { CommandDialog, DataPage } from '@cratis/components';
        import { RegisterAuthor as Register } from '../Registration/RegisterAuthor';
        import { All as Registered } from '../Registration/All';
        import { All } from './All';
        export const AuthorBoard = () => <><DataPage query={Registered} /><CommandDialog command={Register} /></>;
        """;

    const string Books = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        namespace Library.Books.Registration;
        [ReadModel]
        public record Book(string BookId)
        {
            public static IEnumerable<Book> All() => [];
        }
        """;

    void Establish() => Files = DeclaredUserInterfaceFiles.Holding(("Library/Authors/Listing/AuthorBoard.tsx", Component));

    void Because() => Generate(new(),
        ("Library/Authors/Listing/Listing.cs", given.a_cross_slice_screen.Listing),
        ("Library/Authors/Registration/Registration.cs", given.a_cross_slice_screen.Registration.Replace("RegisteredAuthors()", "All()", StringComparison.Ordinal)),
        ("Library/Books/Registration/Registration.cs", Books));

    [Fact] void should_bind_and_round_trip_the_document() => AssertDocument();
    [Fact] void should_write_the_qualified_action() => Says("action Registration.RegisterAuthor").ShouldBeTrue();
    [Fact] void should_state_the_screen_subject_of_the_action() => Says("action Registration.RegisterAuthor for screen").ShouldBeTrue();
    [Fact] void should_disambiguate_the_query_by_feature() => Says("via query Authors.Registration.All").ShouldBeTrue();
    [Fact] void should_keep_the_own_query_bare() => Says("data Author[] via query All").ShouldBeTrue();
    [Fact] void should_not_report_the_read_bindings() => Result.Diagnostics.Where(_ => _.Code == "SP0028" || _.Code == "SP0036").ShouldBeEmpty();
}
