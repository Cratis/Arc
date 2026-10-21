// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_cross_slice_references_with_repeated_slice_names : given.a_cross_slice_screen
{
    void Establish()
    {
        AdditionalSources = [("Library/Books/Registration/Registration.cs", Registration.Replace("Library.Authors", "Library.Books", StringComparison.Ordinal).Replace("Author", "Book", StringComparison.Ordinal).Replace("RegisterBook", "RegisterAuthor", StringComparison.Ordinal))];
        Component = """
            import { CommandDialog } from '@cratis/components';
            import { RegisterAuthor } from '../Registration/RegisterAuthor';
                export const AuthorBoard = () => <CommandDialog command={RegisterAuthor} />;
            """;
    }

    void Because() => GenerateScreen();

    [Fact] void should_bind_and_round_trip_the_document() => AssertDocument();
    [Fact] void should_disambiguate_the_command_by_feature() => Says("action Authors.Registration.RegisterAuthor").ShouldBeTrue();
    [Fact] void should_not_report_the_resolved_command() => Result.Diagnostics.Where(_ => _.Code == "SP0028").ShouldBeEmpty();
}
