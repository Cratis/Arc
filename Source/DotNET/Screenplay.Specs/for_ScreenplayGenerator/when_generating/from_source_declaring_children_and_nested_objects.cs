// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// A child collection and a nested object are the two blocks a projection body nests, and both of them are declared
/// across two types rather than one. Compiling the document they produce and printing it again is what proves the
/// two halves were put back together into something the projection definition language really accepts.
/// </summary>
public class from_source_declaring_children_and_nested_objects : Specification
{
    const string Shelving = """
        using System;
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Projections.ModelBound;

        namespace Library.Inventory.Shelving;

        [EventType]
        public record ShelfInstalled(string Name);

        [EventType]
        public record ShelfLocated(string Aisle);

        [EventType]
        public record BookPlacedOnShelf(Guid ShelfId, Guid BookId, string Title);

        [FromEvent<ShelfLocated>]
        public record ShelfLocation(
            [SetFrom<ShelfLocated>(nameof(ShelfLocated.Aisle))] string Aisle);

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
            IEnumerable<ShelvedBook> Books,
            [Nested] ShelfLocation? Location);
        """;

    static readonly (string Path, string Text)[] _sources = [("Library/Inventory/Shelving/Shelving.cs", Shelving)];

    ScreenplayGenerationResult _result;
    CompilationResult<Cratis.Screenplay.Syntax.ApplicationSyntax> _compiled;
    string _reprinted;

    void Because()
    {
        _result = new ScreenplayGenerator().Generate(Analyzed.Compile(_sources), new ScreenplayOptions());
        _compiled = new ScreenplayCompiler().Compile(_result.Source);
        _reprinted = _compiled.Value is null ? string.Empty : new Cratis.Screenplay.Printing.ScreenplayPrinter().Print(_compiled.Value);
    }

    bool Says(string text) => _result.Source.Contains(text, StringComparison.Ordinal);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(_sources).ShouldBeEmpty();
    [Fact] void should_produce_a_document_that_compiles() => _compiled.Success.ShouldBeTrue();
    [Fact] void should_produce_a_document_the_compiler_says_nothing_about() => _compiled.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _reprinted.ShouldEqual(_result.Source);
    [Fact] void should_write_out_the_children_block() => Says("children books identified by id").ShouldBeTrue();
    [Fact] void should_key_the_children_on_the_event_property() => Says("key bookId").ShouldBeTrue();
    [Fact] void should_find_the_parent_of_a_child() => Says("parent shelfId").ShouldBeTrue();
    [Fact] void should_map_what_the_type_of_the_child_declares() => Says("title = title").ShouldBeTrue();
    [Fact] void should_write_out_the_nested_block() => Says("nested location").ShouldBeTrue();
    [Fact] void should_map_what_the_type_of_the_nested_object_declares() => Says("aisle = aisle").ShouldBeTrue();
    [Fact] void should_report_nothing_as_unmappable() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
