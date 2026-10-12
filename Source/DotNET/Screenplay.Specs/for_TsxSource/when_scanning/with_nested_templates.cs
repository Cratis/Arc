// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens.for_TsxSource.when_scanning;

public class with_nested_templates : Specification
{
    const string Text = "const label = `${a}//${`b${c}`}`; // gone\r\nconst escaped = \"\\\"//kept\";";
    TsxSource _source;

    void Because() => _source = TsxSource.Scan(Text);

    [Fact] void should_keep_templates_and_escaped_quotes() => _source.Code.ShouldEqual(Text.Replace("// gone", "       ", StringComparison.Ordinal));
    [Fact] void should_preserve_original_offsets() => _source.Code.Length.ShouldEqual(Text.Length);
    [Fact] void should_scan_all_boundaries() => _source.Problem.ShouldBeNull();
}
