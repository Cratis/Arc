// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.WrapperEventRoutingAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_WrapperEventRoutingAnalyzer.when_analyzing;

public class with_literals_equal_to_the_command_route : given.a_wrapper_initializer
{
    [Fact]
    async Task should_name_the_redundancy_and_definition_loss() => await VerifyCS.VerifyAnalyzerAsync(Source("""{|#0:{ EventSourceType = "Account", EventStreamType = "Transactions" }|}"""),
        VerifyCS.Diagnostic("ARCCHR0021").WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithArguments("redundant with the command route", "drops the definition"));
}
