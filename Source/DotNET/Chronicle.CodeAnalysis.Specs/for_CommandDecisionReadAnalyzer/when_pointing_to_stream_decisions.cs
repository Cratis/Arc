// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable MA0136 // The fixture intentionally concatenates raw source snippets.
#pragma warning disable SA1117 // Snippet arguments and expected diagnostics are displayed separately.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandDecisionReadAnalyzer;

public class when_pointing_to_stream_decisions
{
    const string Definitions = """
        using System;
        using System.Threading.Tasks;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Chronicle.ReadModels;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Chronicle.Keys;
        using Cratis.Chronicle.Projections.ModelBound;
        using Cratis.Chronicle.ReadModels;
        [EventType("28cae753-4d3c-4e7b-8b24-d60c2f81a906")]
        public record Created;
        [FromEvent<Created>]
        public record State([property: Key] Guid Id);
        """;

    [Fact]
    public async Task should_point_an_unprotected_read_to_stream_decisions() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public Created Handle(State state) => new();
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011")
                .WithSeverity(DiagnosticSeverity.Info).WithArguments("IStreamReads", "StreamDecision.Append", "stream decisions documentation"));

    [Fact]
    public async Task should_point_an_immediate_append_to_stream_decisions() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public async Task<Created> Handle(DecisionRead<State> read, IEventLog log)
                {
                    await log.Append(EventSourceId, new Created());
                    return new Created();
                }
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0012")
                .WithSeverity(DiagnosticSeverity.Info).WithArguments("StreamDecision.Append", "stream decisions documentation"));

    [Fact]
    public async Task should_still_report_an_unprotected_read_when_events_are_returned_with_a_stream_completion() =>
        await AnalyzerVerifier<CommandDecisionReadAnalyzer>.VerifyAnalyzerAsync(Definitions + """
            namespace Cratis.Arc.Chronicle.Streams { public sealed record CompleteStream; }
            [Command]
            public record Create(EventSourceId EventSourceId)
            {
                public (Created, Cratis.Arc.Chronicle.Streams.CompleteStream) Handle(State state) => (new(), new());
            }
            """, AnalyzerVerifier<CommandDecisionReadAnalyzer>.Diagnostic("ARCCHR0011").WithSeverity(DiagnosticSeverity.Info));
}
