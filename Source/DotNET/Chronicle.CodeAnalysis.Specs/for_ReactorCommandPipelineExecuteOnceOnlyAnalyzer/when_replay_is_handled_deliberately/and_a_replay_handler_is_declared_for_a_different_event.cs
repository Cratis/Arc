// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorCommandPipelineExecuteOnceOnlyAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorCommandPipelineExecuteOnceOnlyAnalyzer.when_replay_is_handled_deliberately;

/// <summary>
/// A [Replay] handler only excuses handlers of the same event type — one declared for a different event says
/// nothing about how this one should behave on replay.
/// </summary>
public class and_a_replay_handler_is_declared_for_a_different_event : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Arc.Commands;

namespace TestNamespace
{
    [EventType]
    public record OrderPlaced(string OrderId);
    [EventType]
    public record OrderCancelled(string OrderId);
    public record ChargeCard(string OrderId);

    public class OrderProcessing(ICommandPipeline commandPipeline) : IReactor
    {
        public Task On(OrderPlaced @event) =>
            {|#0:commandPipeline.Execute(new ChargeCard(@event.OrderId))|};

        [Replay]
        public Task OnCancelledReplay(OrderCancelled @event) => Task.CompletedTask;
    }
}",
                VerifyCS.Diagnostic("ARCCHR0006")
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments("On")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
