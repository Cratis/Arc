// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ScreenSyntaxBuilder.when_building;

public class a_screen_with_a_toolbar : given.a_screen_syntax_builder
{
    ScreenSyntax _screen;

    void Because() => _screen = _builder.Build(
        new ScreenModel("AuthorRetirement", "Authors/Retirement/AuthorRetirement.tsx")
        {
            Data = [new("AuthorsToRetire", new TypeReferenceModel("Author", true, false), null)],
            Titles = ["Retire authors"],
            Toolbars = [new("actions", [new("RetireAuthor", "RetireAuthor", "Retire"), new("RetireAuthor2", "RetireAuthor", null)])],
            Tables = [new("Author", [])],
            Actions = ["RegisterAuthor"]
        },
        "Library.Authors.Retirement");

    ScreenToolbarSyntax Toolbar => _screen.Directives.OfType<ScreenToolbarSyntax>().Single();

    [Fact] void should_write_data_then_title_then_toolbar_then_table_then_actions() => _screen.Directives.Select(_ => _.GetType()).ShouldEqual([typeof(ScreenDataSyntax), typeof(ScreenTitleSyntax), typeof(ScreenToolbarSyntax), typeof(ScreenTableSyntax), typeof(ScreenActionSyntax)]);
    [Fact] void should_name_the_toolbar() => Toolbar.Name.ShouldEqual("actions");
    [Fact] void should_normalize_item_names_and_preserve_suffixes() => Toolbar.Items.Select(_ => _.Name).ShouldEqual(["retireAuthor", "retireAuthor2"]);
    [Fact] void should_open_command_input() => Toolbar.Items.First().Kind.ShouldEqual(ToolbarItemKind.Action);
    [Fact] void should_name_the_slice_command() => Toolbar.Items.First().Target.ShouldEqual("RetireAuthor");
    [Fact] void should_preserve_the_literal_label() => Toolbar.Items.First().Label.ShouldEqual("Retire");
    [Fact] void should_omit_an_unread_label() => Toolbar.Items.Last().Label.ShouldBeNull();
    [Fact] void should_not_invent_icons() => Toolbar.Items.First().Icon.ShouldBeNull();
    [Fact] void should_not_invent_parameters() => Toolbar.Items.First().Parameters.ShouldBeEmpty();
    [Fact] void should_not_invent_presentation() => Toolbar.Items.First().Presentation.ShouldBeEmpty();
}
