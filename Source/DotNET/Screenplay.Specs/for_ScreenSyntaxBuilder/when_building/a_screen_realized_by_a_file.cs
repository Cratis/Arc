// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ScreenSyntaxBuilder.when_building;

/// <summary>
/// A screen is written in its <c>file</c> form and never in its declarative one, because what a screen shows and
/// does is written in TypeScript and nothing in a compilation says it.
/// </summary>
public class a_screen_realized_by_a_file : given.a_screen_syntax_builder
{
    ScreenSyntax _screen;

    void Because() => _screen = _builder.Build(
        new ScreenModel("Add-Author", "Authors/Registration/Add-Author.tsx"),
        "Library.Authors.Registration");

    [Fact] void should_name_the_screen_after_the_file() => _screen.Name.ShouldEqual("AddAuthor");
    [Fact] void should_refer_to_the_file() => _screen.File!.Path.ShouldEqual("Authors/Registration/Add-Author.tsx");
    [Fact] void should_state_nothing_about_what_the_screen_shows() => _screen.Directives.ShouldBeEmpty();
}
