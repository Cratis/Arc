// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A screen aggregating several read models is what an Event Modeling screen routinely is, so an import naming a query
/// another slice declares is a binding rather than the noise every other unmatched import is. It cannot be written
/// down - a binding names a query by the bare name its own slice declares it under, and an application declares the
/// same name once per read model - so the screen, the query and the slice declaring it are reported instead of the
/// binding disappearing without a word.
/// </summary>
public class a_screen_importing_a_query_another_slice_declares : Specification
{
    const string Listing = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Authors.Listing;

        [ReadModel]
        public record Author
        {
            public string Id { get; init; } = string.Empty;

            public static IEnumerable<Author> All() => [];
        }
        """;

    const string Lending = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Lending.Loans;

        [ReadModel]
        public record Loan
        {
            public string Id { get; init; } = string.Empty;

            public static IEnumerable<Loan> All() => [];
        }
        """;

    const string Component = """
        import { All } from './Loans';
        import { All as AllAuthors } from '../../Authors/Listing/Listing';
        import { All as AllCatalogs } from '../../Catalogs/Listing/Listing';
        import { DataTable } from 'primereact/datatable';

        export const LoanBoard = () => <DataTable value={[]} />;
        """;

    static readonly DeclaredUserInterfaceFiles _files = DeclaredUserInterfaceFiles.Holding(
        ("Library/Lending/Loans/LoanBoard.tsx", Component));

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(
        _files,
        ("Library/Authors/Listing/Listing.cs", Listing),
        ("Library/Lending/Loans/Loans.cs", Lending));

    ScreenplayDiagnostic Reported =>
        _analysis.Diagnostics.First(_ => _.Code == ScreenplayDiagnosticCodes.CrossSliceQueryBinding);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Authors/Listing/Listing.cs", Listing), ("Library/Lending/Loans/Loans.cs", Lending)).ShouldBeEmpty();
    [Fact] void should_still_bind_the_query_its_own_slice_declares() => _analysis.Model.Slices.First(_ => _.Namespace == "Library.Lending.Loans").Screens.Single().Data.Select(_ => _.Query).ShouldContainOnly(["All"]);
    [Fact] void should_report_the_binding_it_could_not_write_down() => _analysis.Diagnostics.Count(_ => _.Code == ScreenplayDiagnosticCodes.CrossSliceQueryBinding).ShouldEqual(1);
    [Fact] void should_name_the_screen_reading_through_it() => Reported.Message.Contains("'LoanBoard'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_slice_declaring_it() => Reported.Message.Contains("'Library.Authors.Listing'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_locate_it_against_the_slice_the_screen_belongs_to() => Reported.Location.ShouldEqual("Library.Lending.Loans");
    [Fact] void should_say_nothing_of_an_import_resolving_to_no_slice_at_all() => _analysis.Diagnostics.Any(_ => _.Message.Contains("Catalogs", StringComparison.Ordinal)).ShouldBeFalse();
}
