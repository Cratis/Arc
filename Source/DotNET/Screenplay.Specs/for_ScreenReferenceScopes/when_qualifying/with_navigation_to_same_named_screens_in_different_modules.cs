// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenReferenceScopes.when_qualifying;

public class with_navigation_to_same_named_screens_in_different_modules : given.an_application_with_screen_references
{
    void Establish()
    {
        var navigation = new ScreenNavigationModel("Library.Authors.Registration.AuthorDetails", "/authors", new Dictionary<string, string>());
        Model = Model with
        {
            Slices =
            [
                LibraryAuthors.Registration() with { Screens = [new("AuthorDetails", "Authors/AuthorDetails.tsx")] },
                LibraryAuthors.Registration() with { Namespace = "Store.Authors.Registration", Screens = [new("AuthorDetails", "Store/AuthorDetails.tsx")] },
                LibraryAuthors.Listing() with
                {
                    Screens = [new("AuthorBoard", "Authors/AuthorBoard.tsx")
                    {
                        Actions = ["Library.Authors.Registration.RegisterAuthor"],
                        AfterSuccess = new Dictionary<string, ScreenNavigationModel> { ["Library.Authors.Registration.RegisterAuthor"] = navigation },
                        Toolbars = [new("actions", [new("details", string.Empty, null) { Navigation = navigation }])]
                    }]
                }
            ]
        };
        Options = new() { ModulesFromNamespaceRoots = true };
    }

    void Because() => Emit();

    [Fact] void should_qualify_toolbar_navigation_to_the_unique_module() => Emission.Source.ShouldContain("item details navigate to Library.Authors.Registration.AuthorDetails");
    [Fact] void should_qualify_success_navigation_the_same_way() => Emission.Source.ShouldContain("navigate to Library.Authors.Registration.AuthorDetails");
    [Fact] void should_not_report_a_missing_screen() => Emission.Diagnostics.Any(_ => _.Code == "SP0028").ShouldBeFalse();
}
