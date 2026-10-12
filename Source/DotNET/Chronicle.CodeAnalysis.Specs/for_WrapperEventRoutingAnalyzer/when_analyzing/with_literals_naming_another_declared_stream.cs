// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.WrapperEventRoutingAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_WrapperEventRoutingAnalyzer.when_analyzing;

public class with_literals_naming_another_declared_stream : given.a_wrapper_initializer
{
    [Fact]
    async Task should_suggest_typed_routing() => await VerifyCS.VerifyAnalyzerAsync(Source("""{|#0:{ EventSourceType = "Account", EventStreamType = "Adjustments" }|}"""),
        VerifyCS.Diagnostic("ARCCHR0021").WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithArguments("Account.Adjustments", "drops the definition"));
}
