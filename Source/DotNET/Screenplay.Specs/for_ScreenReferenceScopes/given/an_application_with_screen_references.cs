// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenReferenceScopes.given;

public class an_application_with_screen_references : Specification
{
    protected ApplicationModel Model;
    protected ScreenplayOptions Options = new();
    protected ScreenplayEmission Emission;

    void Establish() => Model = LibraryApplication.Build() with
    {
        Slices =
        [
            LibraryAuthors.Registration(),
            LibraryAuthors.Listing() with
            {
                Screens = [new("AuthorBoard", "AuthorBoard.tsx")
                {
                    Actions = ["Library.Authors.Registration.RegisterAuthor"],
                    Toolbars = [new("actions", [new("registerAuthor", "Library.Authors.Registration.RegisterAuthor", null)])],
                    Data = [new("Library.Authors.Registration.Withheld", new("Author", false, true), null)]
                }]
            }
        ]
    };

    protected void Emit() => Emission = new ScreenplayEmitter().Emit(Model, Options);
}
