// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Where components are written against a view model, the component imports the view model and the view model imports
/// the query, so reading the component alone finds a file name and nothing about what the screen reads. The view model
/// belongs to the same slice and sits in the same folder, so what it imports is what the screen reads - and it is
/// followed exactly one hop, because a chain across folders would be inferring an architecture rather than reading one.
/// </summary>
public class a_screen_naming_its_queries_through_a_view_model : Specification
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Lending.Loans;

        [ReadModel]
        public record Loan
        {
            public string Id { get; init; } = string.Empty;

            public static IEnumerable<Loan> All() => [];

            public static IEnumerable<Loan> Overdue() => [];
        }
        """;

    const string Component = """
        import { useLoanBoard } from './LoanBoardViewModel';
        import { DataTable } from 'primereact/datatable';

        export const LoanBoard = () => <DataTable value={useLoanBoard()} />;
        """;

    const string ViewModel = """
        import { All } from './Loans';

        export const useLoanBoard = () => All.use()[0];
        """;

    const string Elsewhere = """
        import { Overdue } from './Loans';

        export const useOverdue = () => Overdue.use()[0];
        """;

    static readonly DeclaredUserInterfaceFiles _files = DeclaredUserInterfaceFiles.Holding(
        ("Library/Lending/Loans/LoanBoard.tsx", Component),
        ("Library/Lending/Loans/LoanBoardViewModel.ts", ViewModel),
        ("Library/Lending/Loans/LoanBoardHelpers.ts", Elsewhere));

    ApplicationModelAnalysis _analysis;

    void Establish() => _analysis = Analyzed.Source(_files, ("Library/Lending/Loans/Loans.cs", Source));

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Lending/Loans/Loans.cs", Source)).ShouldBeEmpty();
    [Fact] void should_bind_the_query_the_view_model_reads_through() => _analysis.Slice().Screens.Single().Data.Select(_ => _.Query).ShouldContainOnly(["All"]);
    [Fact] void should_leave_a_module_that_is_no_view_model_unread() => _analysis.Slice().Screens.Single().Data.Any(_ => _.Query == "Overdue").ShouldBeFalse();
    [Fact] void should_report_only_what_no_screen_states() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.ScreenStructureNotInferred]);
}
