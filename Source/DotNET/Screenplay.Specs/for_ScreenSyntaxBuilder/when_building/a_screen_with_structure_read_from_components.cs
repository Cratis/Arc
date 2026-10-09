// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ScreenSyntaxBuilder.when_building;

/// <summary>
/// The structure read from known components follows the bindings - titles, then tables, then actions - so a screen
/// always reads the same way, and a table is named after the read model under the name the document declares it by.
/// </summary>
public class a_screen_with_structure_read_from_components : given.a_screen_syntax_builder
{
    ScreenSyntax _screen;

    void Because() => _screen = _builder.Build(
        new ScreenModel("AuthorList", "Authors/Listing/AuthorList.tsx")
        {
            Data = [new("AllAuthors", new TypeReferenceModel("Author", true, false), null)],
            Titles = ["Authors"],
            Tables = [new("Author", [new("name", "Name"), new("id", null)])],
            Actions = ["RegisterAuthor"]
        },
        "Library.Authors.Listing");

    ScreenTableSyntax Table => _screen.Directives.OfType<ScreenTableSyntax>().Single();

    [Fact] void should_write_the_bindings_then_titles_then_tables_then_actions() =>
        _screen.Directives.Select(_ => _.GetType()).ShouldEqual([typeof(ScreenDataSyntax), typeof(ScreenTitleSyntax), typeof(ScreenTableSyntax), typeof(ScreenActionSyntax)]);
    [Fact] void should_write_the_title() => _screen.Directives.OfType<ScreenTitleSyntax>().Single().Text.ShouldEqual("Authors");
    [Fact] void should_name_the_table_after_the_read_model() => Table.Target.ShouldEqual("Author");
    [Fact] void should_write_the_columns_in_order() => Table.Columns.Select(_ => _.Property).ShouldEqual(["name", "id"]);
    [Fact] void should_label_a_column_with_its_header() => Table.Columns.First().Label.ShouldEqual("Name");
    [Fact] void should_leave_a_column_without_a_header_unlabeled() => Table.Columns.Last().Label.ShouldBeNull();
    [Fact] void should_not_navigate_from_a_row() => Table.RowClick.ShouldBeNull();
    [Fact] void should_run_the_command() => _screen.Directives.OfType<ScreenActionSyntax>().Single().Command.ShouldEqual("RegisterAuthor");
}
