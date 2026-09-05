// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Attributes and a builder are two ways of saying the same thing, and a projection saying the same thing twice has
/// to be recovered as the same thing twice - otherwise which shape an application happened to be written in would
/// show up in the document, which describes the application rather than the code.
/// </summary>
public class a_child_scope_declared_in_both_shapes : Specification
{
    const string Attributed = """
        using System;
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Library.Catalog.Attributed;

        [EventType]
        public record ShelfInstalled(string Name);

        [EventType]
        public record BookPlacedOnShelf(Guid ShelfId, Guid BookId, string Title);

        public record AttributedBook(
            Guid Id,
            [SetFrom<BookPlacedOnShelf>(nameof(BookPlacedOnShelf.Title))] string Title);

        [ReadModel]
        [FromEvent<ShelfInstalled>]
        public record AttributedShelf(
            Guid Id,
            string Name,
            [ChildrenFrom<BookPlacedOnShelf>(
                key: nameof(BookPlacedOnShelf.BookId),
                identifiedBy: nameof(AttributedBook.Id),
                parentKey: nameof(BookPlacedOnShelf.ShelfId))]
            IEnumerable<AttributedBook> Books);
        """;

    const string Fluent = """
        using System;
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Projections;
        using Library.Catalog.Attributed;

        namespace Library.Catalog.Fluent;

        public record FluentBook
        {
            public Guid Id { get; init; }

            public string Title { get; init; } = string.Empty;
        }

        [ReadModel]
        public record FluentShelf
        {
            public Guid Id { get; init; }

            public string Name { get; init; } = string.Empty;

            public IEnumerable<FluentBook> Books { get; init; } = [];
        }

        public class FluentShelfProjection : IProjectionFor<FluentShelf>
        {
            public void Define(IProjectionBuilderFor<FluentShelf> builder) => builder
                .AutoMap()
                .From<ShelfInstalled>(_ => _.Set(m => m.Name).To(e => e.Name))
                .Children(m => m.Books, c => c
                    .IdentifiedBy(b => b.Id)
                    .From<BookPlacedOnShelf>(_ => _
                        .UsingKey(e => e.BookId)
                        .UsingParentKey(e => e.ShelfId)
                        .Set(m => m.Title).To(e => e.Title)));
        }
        """;

    static readonly (string Path, string Text)[] _sources =
    [
        ("Library/Catalog/Attributed/Attributed.cs", Attributed),
        ("Library/Catalog/Fluent/Fluent.cs", Fluent)
    ];

    ApplicationModelAnalysis _analysis;
    ProjectionChildScopeModel _attributed;
    ProjectionChildScopeModel _fluent;

    void Establish()
    {
        _analysis = Analyzed.Source(_sources);
        _attributed = ChildrenOf("Library.Catalog.Attributed");
        _fluent = ChildrenOf("Library.Catalog.Fluent");
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_hold_the_children_in_the_same_property() => _attributed.Property.ShouldEqual(_fluent.Property);
    [Fact] void should_identify_a_child_the_same_way() => _attributed.IdentifiedBy.ShouldEqual(_fluent.IdentifiedBy);
    [Fact] void should_map_automatically_the_same_way() => _attributed.AutoMap.ShouldEqual(_fluent.AutoMap);
    [Fact] void should_observe_the_same_events() => Block(_attributed).EventTypes.ShouldContainOnly(Block(_fluent).EventTypes);
    [Fact] void should_key_a_child_the_same_way() => Block(_attributed).Key.ShouldEqual(Block(_fluent).Key);
    [Fact] void should_find_the_parent_the_same_way() => Block(_attributed).ParentKey.ShouldEqual(Block(_fluent).ParentKey);
    [Fact] void should_map_the_same_properties() => Mappings(_attributed).ShouldContainOnly(Mappings(_fluent));
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();

    static ProjectionFromModel Block(ProjectionChildScopeModel children) => children.Scope.From.Single();

    static IEnumerable<string> Mappings(ProjectionChildScopeModel children) =>
        Block(children).Properties.Select(_ => $"{_.Key}={_.Value}").Order(StringComparer.Ordinal);

    ProjectionChildScopeModel ChildrenOf(string @namespace) =>
        _analysis.Model.Slices
            .Single(_ => string.Equals(_.Namespace, @namespace, StringComparison.Ordinal))
            .Projections.Single().Scope.Children.Single();
}
