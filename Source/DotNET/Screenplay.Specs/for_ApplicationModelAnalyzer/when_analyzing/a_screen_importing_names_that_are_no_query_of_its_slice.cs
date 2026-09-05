// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// An import is only evidence because it is checked against the model, so every import that does not name a query
/// the slice declares has to leave nothing behind. A package, a component, a command, a type erased before anything
/// runs and a name that merely resembles a query all fail that check for different reasons and all say nothing.
/// </summary>
public class a_screen_importing_names_that_are_no_query_of_its_slice : Specification
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Listing;

        [EventType]
        public record AuthorRetired(string Name);

        [Command]
        public record RetireAuthor(string Name)
        {
            public AuthorRetired Handle() => new(Name);
        }

        [ReadModel]
        public record Author
        {
            public string Id { get; init; } = string.Empty;

            public static IEnumerable<Author> AllAuthors() => [];
        }
        """;

    const string Component = """
        import { DataTable } from 'primereact/datatable';
        import type { AllAuthors } from './AllAuthors';
        import { RetireAuthor } from './RetireAuthor';
        import { AllAuthors as Everyone } from '@cratis/somewhere-else';
        import AllAuthors from './DefaultExport';
        import * as AllAuthors from './Namespace';

        export const AuthorList = () => <DataTable value={[]} />;
        """;

    static readonly DeclaredUserInterfaceFiles _files = DeclaredUserInterfaceFiles.Holding(
        ("Library/Authors/Listing/AuthorList.tsx", Component));

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(_files, ("Library/Authors/Listing/Listing.cs", Source));

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Authors/Listing/Listing.cs", Source)).ShouldBeEmpty();
    [Fact] void should_still_recover_the_screen() => _analysis.Slice().Screens.Select(_ => _.Name).ShouldContainOnly(["AuthorList"]);
    [Fact] void should_bind_nothing() => _analysis.Slice().Screens.Single().Data.ShouldBeEmpty();
    [Fact] void should_report_only_what_no_screen_states() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.ScreenStructureNotInferred]);
}
