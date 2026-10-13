// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ScreenSyntaxBuilder.when_building;

public class a_screen_with_qualified_references : given.a_screen_syntax_builder
{
    ScreenSyntax _screen;

    void Because() => _screen = _builder.Build(new ScreenModel("AuthorBoard", "AuthorBoard.tsx")
    {
        Actions = ["Authors.Registration.RegisterAuthor"],
        Toolbars = [new("actions", [new("RegisterAuthor", "Registration.RegisterAuthor", null)])],
        Data = [new("Registration.All", new("Author", true, false), null)]
    },
    "Library.Authors.Listing");

    [Fact] void should_keep_the_action_qualified() => _screen.Directives.OfType<ScreenActionSyntax>().Single().Command.ShouldEqual("Authors.Registration.RegisterAuthor");
    [Fact] void should_keep_the_toolbar_target_qualified() => _screen.Directives.OfType<ScreenToolbarSyntax>().Single().Items.Single().Target.ShouldEqual("Registration.RegisterAuthor");
    [Fact] void should_keep_the_query_qualified() => _screen.Directives.OfType<ScreenDataSyntax>().Single().Query.ShouldEqual("Registration.All");
}
