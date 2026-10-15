// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // Independently readable source fragments are concatenated.

using Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes;
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_UseEventSourceDefinitionCodeFixProvider;

public class when_replacing_legacy_attributes
{
    const string Definitions = "using Cratis.Arc.Commands.ModelBound; using Cratis.Chronicle.Events; using Cratis.Chronicle.EventSources; [EventSource, EventStream(\"Transactions\")] public class AccountEventSource : IEventSource; ";

    [Fact]
    public Task should_replace_a_source_attribute() => CodeFixVerifier<UseEventSourceDefinitionAnalyzer, UseEventSourceDefinitionCodeFixProvider>.VerifyCodeFixAsync(
        Definitions + "[Command, EventSourceType(\"Account\")] public record Deposit { public void Handle() { } }",
        Definitions + "[Command, global::Cratis.Arc.Chronicle.Commands.EventSource<global::AccountEventSource>] public record Deposit { public void Handle() { } }",
        Info());

    [Fact]
    public Task should_combine_a_source_and_stream_in_the_same_attribute_list() => CodeFixVerifier<UseEventSourceDefinitionAnalyzer, UseEventSourceDefinitionCodeFixProvider>.VerifyCodeFixAsync(
        Definitions + "[Command, EventSourceType(\"Account\"), EventStreamType(\"Transactions\")] public record Deposit { public void Handle() { } }",
        Definitions + "[Command, global::Cratis.Arc.Chronicle.Commands.EventSource<global::AccountEventSource>(\"Transactions\")] public record Deposit { public void Handle() { } }",
        Info());

    [Fact]
    public Task should_leave_an_unmatched_source_alone() => AnalyzerVerifier<UseEventSourceDefinitionAnalyzer>.VerifyAnalyzerAsync(
        Definitions + "[Command, EventSourceType(\"Another\")] public record Deposit { public void Handle() { } }");

    static DiagnosticBuilder Info() => AnalyzerVerifier<UseEventSourceDefinitionAnalyzer>.Diagnostic("ARCCHR0013").WithSeverity(DiagnosticSeverity.Info);
}
