// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorCommandPipelineExecuteOnceOnlyAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorCommandPipelineExecuteOnceOnlyAnalyzer.when_replay_is_handled_deliberately;

/// <summary>
/// An inherited replay handler does not excuse a command executed for another event type.
/// </summary>
public class and_an_inherited_replay_handler_is_for_another_event : Specification
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

    public class ReplayHandlers
    {
        [Replay]
        public Task OnReplay(OrderCancelled @event) => Task.CompletedTask;
    }

    public class OrderProcessing(ICommandPipeline commandPipeline) : ReplayHandlers, IReactor
    {
        public Task On(OrderPlaced @event) =>
            {|#0:commandPipeline.Execute(new ChargeCard(@event.OrderId))|};
    }
}",
                VerifyCS.Diagnostic("ARCCHR0006")
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments("'On' invokes")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
