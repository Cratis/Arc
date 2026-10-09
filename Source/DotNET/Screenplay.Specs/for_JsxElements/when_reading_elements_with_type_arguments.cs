// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Screens;

namespace Cratis.Arc.Screenplay.for_JsxElements;

/// <summary>
/// An element written with type arguments - <c>&lt;Column&lt;Issue&gt; ... /&gt;</c> - is the same element as one
/// written without, and is read as such rather than skipped.
/// </summary>
public class when_reading_elements_with_type_arguments : Specification
{
    const string Text = """
        <Column<Issue> field="number" header="No" />
        <Column<Map<string, Issue[]>, (a: A) => B> field="map" />
        <Column<Issue> field="inner" body={(issue: Issue) => <b>{issue.title}</b>}>
            <Column<Nested> field="child" />
        </Column>
        <Column<Broken field="broken" />
        """;

    List<JsxElement> _columns;

    void Because() => _columns = [.. JsxElements.Named(Text, "Column")];

    [Fact] void should_find_every_outermost_element() => _columns.Count.ShouldEqual(4);
    [Fact] void should_read_the_attributes_after_the_type_arguments() => _columns[0]["field"]!.Literal.ShouldEqual("number");
    [Fact] void should_read_the_header_after_the_type_arguments() => _columns[0]["header"]!.Literal.ShouldEqual("No");
    [Fact] void should_skip_nested_and_arrow_type_arguments() => _columns[1]["field"]!.Literal.ShouldEqual("map");
    [Fact] void should_read_an_element_with_type_arguments_in_full() => _columns.Take(3).All(_ => _.IsReadable).ShouldBeTrue();
    [Fact] void should_keep_a_nested_element_with_type_arguments_in_the_children() => JsxElements.Named(_columns[2].Children, "Column").Single()["field"]!.Literal.ShouldEqual("child");
    [Fact] void should_mark_type_arguments_that_are_never_closed_unreadable() => _columns[3].IsReadable.ShouldBeFalse();
}
