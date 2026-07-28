// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ScreenSyntaxBuilder.when_building;

/// <summary>
/// A screen carries its bindings and its file together - the grammar allows both, the bindings state what the screen
/// reads, and the file stays the pointer to the implementation no directive replaces. The optional marker is the one
/// thing that has to go: a data directive has no room for it, and a document carrying one does not compile.
/// </summary>
public class a_screen_bound_to_queries : given.a_screen_syntax_builder
{
    ScreenSyntax _screen;

    void Because() => _screen = _builder.Build(
        new ScreenModel("AuthorList", "Authors/Listing/AuthorList.tsx")
        {
            Data =
            [
                new("AuthorById", new TypeReferenceModel("Author", false, true), "Id"),
                new("AllAuthors", new TypeReferenceModel("Author", true, false), null)
            ]
        },
        "Library.Authors.Listing");

    ScreenDataSyntax Binding(int index) => _screen.Directives.OfType<ScreenDataSyntax>().ElementAt(index);

    [Fact] void should_refer_to_the_file_realizing_it() => _screen.File!.Path.ShouldEqual("Authors/Listing/AuthorList.tsx");
    [Fact] void should_write_a_directive_per_binding() => _screen.Directives.Count().ShouldEqual(2);
    [Fact] void should_order_the_bindings_by_query() => _screen.Directives.OfType<ScreenDataSyntax>().Select(_ => _.Query).ShouldEqual(["AllAuthors", "AuthorById"]);
    [Fact] void should_keep_a_collection_a_collection() => Binding(0).Type.IsCollection.ShouldBeTrue();
    [Fact] void should_state_no_key_for_a_query_requiring_none() => Binding(0).By.ShouldBeNull();
    [Fact] void should_camel_case_the_key_a_binding_is_read_by() => Binding(1).By.ShouldEqual("id");
    [Fact] void should_drop_the_optional_marker_a_data_directive_cannot_carry() => Binding(1).Type.IsOptional.ShouldBeFalse();
    [Fact] void should_keep_naming_the_type_the_query_returns() => Binding(1).Type.Name.ShouldEqual("Author");
}
