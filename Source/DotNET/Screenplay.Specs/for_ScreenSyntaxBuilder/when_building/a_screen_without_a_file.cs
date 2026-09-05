// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ScreenSyntaxBuilder.when_building;

/// <summary>
/// A screen with neither a file nor a directive has an empty body and does not compile, so a path is always
/// resolved - falling back to where the vertical slice convention would put the file.
/// </summary>
public class a_screen_without_a_file : given.a_screen_syntax_builder
{
    ScreenSyntax _screen;

    void Because() => _screen = _builder.Build(new ScreenModel("AddAuthor", string.Empty), "Library.Authors.Registration");

    [Fact] void should_still_name_the_screen() => _screen.Name.ShouldEqual("AddAuthor");
    [Fact] void should_fall_back_to_where_the_convention_puts_it() => _screen.File!.Path.ShouldEqual("Authors/Registration/AddAuthor.tsx");
}
