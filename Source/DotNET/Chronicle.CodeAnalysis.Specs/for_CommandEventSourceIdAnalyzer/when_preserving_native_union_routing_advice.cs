// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using static Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.NativeUnionSource;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandEventSourceIdAnalyzer;

public class when_preserving_native_union_routing_advice : Specification
{
    [Fact]
    public Task should_keep_wrapper_routing_diagnostics_inside_union_case_construction() => Verify<WrapperEventRoutingAnalyzer>(
        Imports + "using Cratis.Chronicle.EventSources; public union Outcome(EventForEventSourceId, int); " +
        "[EventSource] public class Account : IEventSource; public class Factory { public Outcome Create() => new EventForEventSourceId(\"id\", new object()) { EventSourceType = new EventSourceType(\"Account\"), EventStreamType = new(\"Typo\") }; }",
        new ExpectedDiagnostic("ARCCHR0020", DiagnosticSeverity.Warning, "Typo", "Account"));

    [Fact]
    public Task should_keep_refusal_callback_validation_inside_union_returning_handlers() => Verify<RefusalBranchReturnAnalyzer>(
        Imports + "using Cratis.Arc.Chronicle.Reactors; public union Outcome(E, Failure); [EventType] public record E; public record Failure; " +
        "[Command] public record Work { public Outcome Handle() => new E(); } public class Reactor { public CommandInvocation Call() => Invoke.Command(new Work()).OnRefused(Refused.Any, _ => new Work()); }",
        new ExpectedDiagnostic("ARCCHR0027", DiagnosticSeverity.Warning, "registered events", "Acknowledge"));
}
#endif
