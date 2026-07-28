// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// There is nothing to search an event for when the parent carries no identifier of its own, and inventing one from
/// whichever property happened to look right would attach children to the wrong parent. The event source is what
/// remains, which is also what really happens, so nothing is written out at all.
/// </summary>
public class a_child_collection_whose_parent_carries_no_identifier : Specification
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
        public record BookPlacedOnShelf(Guid BookId, string Title);

        public record ShelvedBook(
            Guid Id,
            [SetFrom<BookPlacedOnShelf>(nameof(BookPlacedOnShelf.Title))] string Title);

        [ReadModel]
        [FromEvent<ShelfInstalled>]
        public record Shelf(
            string Name,
            [ChildrenFrom<BookPlacedOnShelf>(key: nameof(BookPlacedOnShelf.BookId))]
            IEnumerable<ShelvedBook> Books);
        """;

    ApplicationModelAnalysis _analysis;
    Model.ProjectionFromModel _from;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _from = _analysis.Slice().Projection!.Scope.Children.Single().Scope.From.Single();
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_leave_the_parent_to_the_event_source() => _from.ParentKey.ShouldBeNull();
    [Fact] void should_still_key_the_child_on_what_the_declaration_names() => _from.Key.ShouldEqual("bookId");
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
