// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelParser;

public class when_compiling_a_document_with_an_unpopulated_field : given.a_document_with_an_unpopulated_field
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source);

    [Fact] void should_fail_coverage_validation() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_unpopulated_field() => _result.Diagnostics.Select(_ => _.Code).ShouldContain("PLAY0284");
    [Fact] void should_keep_the_syntax_for_the_board() => _result.Value.ShouldNotBeNull();
}
