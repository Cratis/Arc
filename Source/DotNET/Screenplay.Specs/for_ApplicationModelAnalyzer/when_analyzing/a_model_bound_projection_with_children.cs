// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A child collection is only half declared where it is written - the property says which event brings an instance
/// into being and how it is keyed, while the type it holds says what that event fills in. Both halves describe the
/// same block, so reading only the property would yield children that are never given any content.
/// </summary>
public class a_model_bound_projection_with_children : Specification
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
            [ChildrenFrom<BookPlacedOnShelf>(
                key: nameof(BookPlacedOnShelf.BookId),
                parentKey: nameof(BookPlacedOnShelf.ShelfId))]
            IEnumerable<ShelvedBook> Books);
        """;

    ApplicationModelAnalysis _analysis;
    ProjectionChildScopeModel _children;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _children = _analysis.Slice().Projections.Single().Scope.Children.Single();
    }

    ProjectionFromModel From => _children.Scope.From.Single();

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_hold_the_children_in_the_property_declaring_them() => _children.Property.ShouldEqual("Books");
    [Fact] void should_identify_a_child_by_the_property_its_type_names() => _children.IdentifiedBy.ShouldEqual("id");
    [Fact] void should_leave_automatic_mapping_to_the_enclosing_scope() => _children.AutoMap.ShouldEqual(ProjectionAutoMapMode.Inherit);
    [Fact] void should_observe_the_event_bringing_a_child_into_being() => From.EventTypes.ShouldContainOnly(["BookPlacedOnShelf"]);
    [Fact] void should_key_a_child_on_the_property_the_declaration_names() => From.Key.ShouldEqual("bookId");
    [Fact] void should_find_the_parent_by_the_property_the_declaration_names() => From.ParentKey.ShouldEqual("shelfId");
    [Fact] void should_map_what_the_type_of_the_child_declares() => From.Properties["Title"].ShouldEqual("title");
    [Fact] void should_still_observe_the_event_the_read_model_names() => _analysis.Slice().Projections.Single().Scope.From.Single().EventTypes.ShouldContainOnly(["ShelfInstalled"]);
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
