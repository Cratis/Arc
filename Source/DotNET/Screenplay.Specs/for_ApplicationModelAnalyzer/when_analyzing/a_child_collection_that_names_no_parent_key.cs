// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A declaration naming no parent key does not mean the parent is the event source - the event is searched for a
/// property carrying the same kind of value the parent is identified by, and that is what the child is attached by.
/// A document saying nothing here would describe a hierarchy assembled differently from the one that really runs.
/// </summary>
public class a_child_collection_that_names_no_parent_key : Specification
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

        public record ShelvedBook(
            Guid Id,
            [SetFrom<BookPlacedOnShelf>(nameof(BookPlacedOnShelf.Title))] string Title);

        [ReadModel]
        [FromEvent<ShelfInstalled>]
        public record Shelf(
            Guid Id,
            string Name,
            [ChildrenFrom<BookPlacedOnShelf>(key: nameof(BookPlacedOnShelf.BookId))]
            IEnumerable<ShelvedBook> Books);
        """;

    ApplicationModelAnalysis _analysis;
    Model.ProjectionFromModel _from;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _from = _analysis.Slice().Projections.Single().Scope.Children.Single().Scope.From.Single();
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_find_the_parent_by_the_property_carrying_its_identifier() => _from.ParentKey.ShouldEqual("shelfId");
    [Fact] void should_still_key_the_child_on_what_the_declaration_names() => _from.Key.ShouldEqual("bookId");
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
