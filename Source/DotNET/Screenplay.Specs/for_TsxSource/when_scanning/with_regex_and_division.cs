// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens.for_TsxSource.when_scanning;

public class with_regex_and_division : Specification
{
    const string Text = """
        const regex = /https?:\/\//g; // c
        const classes = /[/'<>{}]+/;
        const divided = a / b; // c
        const view = <Page body={x => /[}/]/.test(x) ? x / 2 : x} />;
        """;

    TsxSource _source;

    void Because() => _source = TsxSource.Scan(Text);

    [Fact] void should_preserve_regexes_and_division() => _source.Code.ShouldEqual(Text.Replace("// c", "    ", StringComparison.Ordinal));
    [Fact] void should_ignore_tag_shapes_in_regexes() => _source.Tags.Select(_ => _.Name).ShouldEqual(["Page"]);
    [Fact] void should_match_the_attribute_expression() => JsxElements.Named(_source, "Page").Single().IsReadable.ShouldBeTrue();
    [Fact] void should_scan_all_boundaries() => _source.Problem.ShouldBeNull();
}
