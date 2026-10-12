// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens.for_JsxElements;

public class when_reading_elements_holding_double_slashes : Specification
{
    const string Text = """
        <Page title="https://example.com" label={`a//b`} fake="<Page title='fake'/>">
            See http://x // y. Don't stop.
            <Page.Item label="Don't" />
            {`outer${`inner${value}`}//text`}
        </Page>
        """;

    List<JsxElement> _pages;
    List<JsxElement> _items;

    void Because()
    {
        _pages = JsxElements.Named(Text, "Page").ToList();
        _items = JsxElements.Named(Text, "Page.Item").ToList();
    }

    [Fact] void should_preserve_the_url() => _pages[0]["title"].Literal.ShouldEqual("https://example.com");
    [Fact] void should_preserve_the_template() => _pages[0]["label"].Literal.ShouldEqual("a//b");
    [Fact] void should_read_the_item() => _items[0].IsReadable.ShouldBeTrue();
    [Fact] void should_not_read_a_tag_in_an_attribute() => _pages.Count.ShouldEqual(1);
}
