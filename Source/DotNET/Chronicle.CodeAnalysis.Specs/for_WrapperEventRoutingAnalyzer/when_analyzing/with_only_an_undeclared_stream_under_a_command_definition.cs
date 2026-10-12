// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.WrapperEventRoutingAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_WrapperEventRoutingAnalyzer.when_analyzing;

public class with_only_an_undeclared_stream_under_a_command_definition : given.a_wrapper_initializer
{
    [Fact]
    async Task should_warn_for_legacy_stream_routing() => await VerifyCS.VerifyAnalyzerAsync(Source("""{|#0:{ EventStreamType = "Typo" }|}"""),
        VerifyCS.Diagnostic("ARCCHR0020").WithSeverity(DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Typo", "AccountEventSource"));
    [Fact]
    async Task should_warn_for_typed_stream_routing() => await VerifyCS.VerifyAnalyzerAsync(Source("""{|#0:{ EventStream = "Typo" }|}"""),
        VerifyCS.Diagnostic("ARCCHR0020").WithSeverity(DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Typo", "AccountEventSource"));
}
