// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Screens;

namespace Cratis.Arc.Screenplay.for_JsxElements;

/// <summary>
/// The reader follows one shape and marks everything else unreadable rather than reading it approximately - which is
/// what lets a screen leave out what it did not understand instead of stating something its component does not.
/// </summary>
public class when_reading_elements : Specification
{
    const string Text = """
        <Page title="Authors" query={AllAuthors} label={`Plain`} computed={`Hello ${name}`} entity="a &amp; b" wide>
            <Page.Item label='Retire' command={() => { retire({ id: "}" }); }} />
            <Page title="Nested" />
        </Page>
        <Page {...props} title="Spread" />
        <Pager title="Not a page" />
        <Page title="Unclosed"
        """;

    List<JsxElement> _pages;
    JsxElement _first;

    void Because()
    {
        _pages = [.. JsxElements.Named(Text, "Page")];
        _first = _pages[0];
    }

    [Fact] void should_find_only_outermost_elements_with_the_tag() => _pages.Select(_ => _["title"]?.Literal).ShouldEqual(["Authors", "Spread", null]);
    [Fact] void should_read_a_quoted_attribute_as_text() => _first["title"]!.Literal.ShouldEqual("Authors");
    [Fact] void should_read_a_name_between_braces_as_an_identifier() => _first["query"]!.Identifier.ShouldEqual("AllAuthors");
    [Fact] void should_read_a_template_without_interpolation_as_text() => _first["label"]!.Literal.ShouldEqual("Plain");
    [Fact] void should_not_read_an_interpolated_template_as_text() => _first["computed"]!.Literal.ShouldBeNull();
    [Fact] void should_not_decode_a_character_reference() => _first["entity"]!.Literal.ShouldBeNull();
    [Fact] void should_read_a_bare_attribute_as_set() => _first["wide"]!.Written.ShouldEqual("true");
    [Fact] void should_keep_the_nested_element_in_the_children() => JsxElements.Named(_first.Children, "Page").Single()["title"]!.Literal.ShouldEqual("Nested");
    [Fact] void should_read_a_dotted_tag_with_braces_and_strings_in_its_attributes() => JsxElements.Named(_first.Children, "Page.Item").Single()["label"]!.Literal.ShouldEqual("Retire");
    [Fact] void should_read_the_first_element_in_full() => _first.IsReadable.ShouldBeTrue();
    [Fact] void should_mark_an_element_with_spread_attributes_unreadable() => _pages[1].IsReadable.ShouldBeFalse();
    [Fact] void should_mark_an_element_that_is_never_closed_unreadable() => _pages[2].IsReadable.ShouldBeFalse();
}
