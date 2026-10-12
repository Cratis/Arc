// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.WrapperEventRoutingAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_WrapperEventRoutingAnalyzer.when_analyzing;

public class with_a_typed_undeclared_stream : given.a_wrapper_initializer
{
    [Fact]
    async Task should_warn() => await VerifyCS.VerifyAnalyzerAsync(Source("""{|#0:{ EventSource = typeof(AccountEventSource), EventStream = "Typo" }|}"""),
        VerifyCS.Diagnostic("ARCCHR0020").WithSeverity(DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Typo", "AccountEventSource"));
}
