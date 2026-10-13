// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_own_slice_imports_in_a_shared_source_directory : given.a_generated_document_without_executable_binding
{
    const string Component = """
        import { CommandDialog, DataPage } from '@cratis/components';
        import { RegisterAuthor } from './RegisterAuthor';
        import { RegisteredAuthors } from './RegisteredAuthors';
        export const AuthorBoard = () => <><DataPage query={RegisteredAuthors} /><CommandDialog command={RegisterAuthor} /></>;
        """;

    void Establish() => Files = DeclaredUserInterfaceFiles.Holding(("Library/Authors/Shared/AuthorBoard.tsx", Component));

    void Because() => Generate(new(),
        ("Library/Authors/Shared/Listing.cs", given.a_cross_slice_screen.Registration.Replace("Library.Authors.Registration", "Library.Authors.Listing", StringComparison.Ordinal)),
        ("Library/Authors/Shared/Registration.cs", given.a_cross_slice_screen.Registration));

    [Fact] void should_bind_and_round_trip_the_document() => AssertDocument();
    [Fact] void should_keep_each_screens_own_command_bare() => Result.Model.Slices.SelectMany(_ => _.Screens).SelectMany(_ => _.Actions).ShouldContainOnly(["RegisterAuthor", "RegisterAuthor"]);
    [Fact] void should_keep_each_screens_own_query_bare() => Result.Model.Slices.SelectMany(_ => _.Screens).SelectMany(_ => _.Data).Select(_ => _.Query).ShouldContainOnly(["RegisteredAuthors", "RegisteredAuthors"]);
    [Fact] void should_write_the_bare_action() => Says("action RegisterAuthor").ShouldBeTrue();
    [Fact] void should_write_the_bare_query() => Says("data RegisteredAuthor[] via query RegisteredAuthors").ShouldBeTrue();
    [Fact] void should_not_report_an_unresolved_binding() => Result.Diagnostics.Where(_ => _.Code == "SP0028").ShouldBeEmpty();
}
