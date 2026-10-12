// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens.for_TsxSource.when_scanning;

public class with_a_string_continued_across_a_crlf_line_break : Specification
{
    const string Text = "const value = \"first \\\r\nsecond\"; // gone";
    TsxSource _source;
    void Because() => _source = TsxSource.Scan(Text);
    [Fact] void should_have_no_problem() => _source.Problem.ShouldBeNull();
    [Fact] void should_keep_the_string() => _source.Code.ShouldContain("\"first \\\r\nsecond\"");
    [Fact] void should_blank_the_comment() => _source.Code.ShouldNotContain("gone");
}
