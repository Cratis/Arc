// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// What removes an instance is read from the type of the instance, alongside the events that fill it in. The same
/// removal written beside the collection instead is a form nothing reads yet, and a child collection nothing ever
/// removes anything from is exactly the sort of quiet difference a document is supposed to make impossible.
/// </summary>
public class a_child_removed_by_an_event : Specification
{
    const string Source = """
        using System;
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Library.Inventory.Shelving;

        [EventType]
        public record ShelfInstalled(string Name);

        [EventType]
        public record BookPlacedOnShelf(Guid ShelfId, Guid BookId, string Title);

        [EventType]
        public record BookTakenOffShelf(Guid BookId);

        [RemovedWith<BookTakenOffShelf>(key: nameof(BookTakenOffShelf.BookId))]
        public record ShelvedBook(
            Guid Id,
            [SetFrom<BookPlacedOnShelf>(nameof(BookPlacedOnShelf.Title))] string Title);

        [ReadModel]
        [FromEvent<ShelfInstalled>]
        public record Shelf(
            Guid Id,
            string Name,
            [ChildrenFrom<BookPlacedOnShelf>(
                key: nameof(BookPlacedOnShelf.BookId),
                parentKey: nameof(BookPlacedOnShelf.ShelfId))]
            [RemovedWith<BookTakenOffShelf>(key: nameof(BookTakenOffShelf.BookId))]
            IEnumerable<ShelvedBook> Books);
        """;

    ApplicationModelAnalysis _analysis;
    Model.ProjectionChildScopeModel _children;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _children = _analysis.Slice().Projection!.Scope.Children.Single();
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_remove_a_child_when_the_type_of_the_child_says_so() => _children.Scope.RemovedWith.Single().EventType.ShouldEqual("BookTakenOffShelf");
    [Fact] void should_key_the_removal_on_what_the_declaration_names() => _children.Scope.RemovedWith.Single().Key.ShouldEqual("bookId");
    [Fact] void should_report_the_removal_declared_beside_the_collection() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContain(ScreenplayDiagnosticCodes.UnmappableProjectionScope);
    [Fact] void should_say_where_the_removal_is_read_from_instead() => _analysis.Diagnostics.Any(_ => _.Message.Contains("only when the type of the child declares it", StringComparison.Ordinal)).ShouldBeTrue();
}
