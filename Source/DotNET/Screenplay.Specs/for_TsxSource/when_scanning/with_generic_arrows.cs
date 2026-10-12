// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens.for_TsxSource.when_scanning;

public class with_generic_arrows : Specification
{
    const string Text = "const first = <T,>(x: T) => x; const second = <T extends Value>(x: T) => x;";
    TsxSource _source;

    void Because() => _source = TsxSource.Scan(Text);

    [Fact] void should_not_find_jsx_tags() => _source.Tags.ShouldBeEmpty();
    [Fact] void should_scan_all_boundaries() => _source.Problem.ShouldBeNull();
}
