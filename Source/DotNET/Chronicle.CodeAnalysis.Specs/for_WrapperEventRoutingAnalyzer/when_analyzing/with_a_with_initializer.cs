// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.WrapperEventRoutingAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_WrapperEventRoutingAnalyzer.when_analyzing;

public class with_a_with_initializer : given.a_wrapper_initializer
{
    [Fact]
    async Task should_warn() => await VerifyCS.VerifyAnalyzerAsync(Source("""{|#0:{ EventSource = typeof(AccountEventSource), EventStream = "Typo" }|}""", creation: "new EventForEventSourceId(\"id\", new object()) with"),
        VerifyCS.Diagnostic("ARCCHR0020").WithSeverity(DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Typo", "AccountEventSource"));
    [Fact]
    async Task should_suggest_typed_routing() => await VerifyCS.VerifyAnalyzerAsync(Source("""{|#0:{ EventSourceType = "Account", EventStreamType = "Adjustments" }|}""", creation: "new EventForEventSourceId(\"id\", new object()) with"),
        VerifyCS.Diagnostic("ARCCHR0021").WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithArguments("Account.Adjustments"));
}
