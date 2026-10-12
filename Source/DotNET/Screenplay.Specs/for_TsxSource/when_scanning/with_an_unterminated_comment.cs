// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens.for_TsxSource.when_scanning;

public class with_an_unterminated_comment : Specification
{
    TsxSource _source;
    void Because() => _source = TsxSource.Scan("\n/* never closed");
    [Fact] void should_report_the_boundary() => _source.Problem.ShouldEqual((2, "block comment"));
}
