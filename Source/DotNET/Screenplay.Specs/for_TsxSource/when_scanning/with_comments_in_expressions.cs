// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens.for_TsxSource.when_scanning;

public class with_comments_in_expressions : Specification
{
    const string Text = "const view = <Page>{/* <Column field=\"x\"/> */}</Page>;\n// import { A } from './A';";
    TsxSource _source;

    void Because() => _source = TsxSource.Scan(Text);

    [Fact] void should_not_find_commented_tags() => _source.Tags.Select(_ => _.Name).ShouldEqual(["Page"]);
    [Fact] void should_blank_comments_without_moving_the_newline() => _source.Code.IndexOf('\n', StringComparison.Ordinal).ShouldEqual(Text.IndexOf('\n', StringComparison.Ordinal));
    [Fact] void should_blank_the_import_comment() => _source.Code.Contains("import", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_count_a_comment_as_content() => _source.Tags[0].HasContent.ShouldBeFalse();
    [Fact] void should_scan_all_boundaries() => _source.Problem.ShouldBeNull();
}
