// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorCommandPipelineExecuteOnceOnlyAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorCommandPipelineExecuteOnceOnlyAnalyzer.when_replay_is_handled_deliberately;

/// <summary>
/// The exact shape reported as a false positive in #2632: a live handler invokes
/// <c>ICommandPipeline.Execute</c> without [OnceOnly], but a [Replay] handler is declared for the same event
/// type and takes over during replay — that is itself the statement that replay was considered.
/// </summary>
public class and_a_replay_handler_is_declared_for_the_same_event : Specification
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
    public record ChargeCard(string OrderId);

    public class OrderProcessing(ICommandPipeline commandPipeline) : IReactor
    {
        public Task On(OrderPlaced @event) =>
            commandPipeline.Execute(new ChargeCard(@event.OrderId));

        [Replay]
        public Task OnReplay(OrderPlaced @event) => Task.CompletedTask;
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
