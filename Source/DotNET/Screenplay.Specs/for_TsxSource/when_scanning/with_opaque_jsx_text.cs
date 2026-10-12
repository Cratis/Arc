// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens.for_TsxSource.when_scanning;

public class with_opaque_jsx_text : Specification
{
    const string Text = """
        const view = <Page title="https://x" href='//cdn/a' fake="<Fake/>">
            See http://x // y. Don't stop.
            {value => <i>Don't</i>}
        </Page>;
        """;

    TsxSource _source;

    void Because() => _source = TsxSource.Scan(Text);

    [Fact] void should_keep_opaque_text_verbatim() => _source.Code.ShouldEqual(Text);
    [Fact] void should_scan_all_boundaries() => _source.Problem.ShouldBeNull();
    [Fact] void should_find_only_real_tags() => _source.Tags.Select(_ => _.Name).ShouldEqual(["Page", "i"]);
}
