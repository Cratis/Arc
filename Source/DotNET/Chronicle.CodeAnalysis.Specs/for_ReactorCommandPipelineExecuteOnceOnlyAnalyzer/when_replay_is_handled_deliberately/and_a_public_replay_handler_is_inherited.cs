// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorCommandPipelineExecuteOnceOnlyAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorCommandPipelineExecuteOnceOnlyAnalyzer.when_replay_is_handled_deliberately;

/// <summary>
/// A public replay handler inherited from a base reactor is a valid replay decision for a derived live handler.
/// </summary>
public class and_a_public_replay_handler_is_inherited : Specification
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

    public class ReplayHandlers
    {
        [Replay]
        public Task OnReplay(OrderPlaced @event) => Task.CompletedTask;
    }

    public class OrderProcessing(ICommandPipeline commandPipeline) : ReplayHandlers, IReactor
    {
        public Task On(OrderPlaced @event) =>
            commandPipeline.Execute(new ChargeCard(@event.OrderId));
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
