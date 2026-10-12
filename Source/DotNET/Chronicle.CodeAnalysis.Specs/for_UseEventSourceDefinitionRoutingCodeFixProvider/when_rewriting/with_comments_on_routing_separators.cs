// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.CodeFixVerifier<Cratis.Arc.Chronicle.CodeAnalysis.WrapperEventRoutingAnalyzer, Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes.UseEventSourceDefinitionRoutingCodeFixProvider>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_UseEventSourceDefinitionRoutingCodeFixProvider.when_rewriting;

public class with_comments_on_routing_separators : for_WrapperEventRoutingAnalyzer.given.a_wrapper_initializer
{
    [Fact]
    async Task should_keep_the_comments() => await VerifyCS.VerifyCodeFixAsync(
        Source(@"{|#0:{
        EventSourceType = ""Account"", // Keep the account route.
        EventStreamType = ""Adjustments""
    }|}"),
        Source(@"
    {
        EventSource = typeof(global::AccountEventSource), // Keep the account route.
        EventStream = ""Adjustments""
    }"),
        new ExpectedDiagnostic("ARCCHR0021", DiagnosticSeverity.Info, "Account.Adjustments"));
}
