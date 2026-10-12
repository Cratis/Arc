// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens.for_TsxSource.when_scanning;

public class with_plain_typescript_assertions : Specification
{
    const string Text = """
        const value = <Value>item;
        import { A } from './A';
        """;

    TsxSource _source;

    void Because() => _source = TsxSource.Scan(Text, jsx: false);

    [Fact] void should_not_find_a_jsx_tag() => _source.Tags.ShouldBeEmpty();
    [Fact] void should_read_imports_after_type_assertions() => ScreenImports.Statements(_source).Select(_ => _.Name).ShouldEqual(["A"]);
    [Fact] void should_scan_all_boundaries() => _source.Problem.ShouldBeNull();
}
