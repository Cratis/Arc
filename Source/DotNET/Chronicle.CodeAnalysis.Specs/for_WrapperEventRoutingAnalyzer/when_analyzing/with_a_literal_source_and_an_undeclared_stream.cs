// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.WrapperEventRoutingAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_WrapperEventRoutingAnalyzer.when_analyzing;

public class with_a_literal_source_and_an_undeclared_stream : given.a_wrapper_initializer
{
    [Fact]
    async Task should_warn_even_outside_a_command() => await VerifyCS.VerifyAnalyzerAsync(Source("""{|#0:{ EventSourceType = new EventSourceType("Account"), EventStreamType = new("Typo") }|}""", ""),
        VerifyCS.Diagnostic("ARCCHR0020").WithSeverity(DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Typo", "AccountEventSource"));
}
