// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorCommandPipelineExecuteOnceOnlyAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorCommandPipelineExecuteOnceOnlyAnalyzer.when_replay_is_handled_deliberately;

/// <summary>
/// A handler marked [Replay] is itself excused regardless of what it does — declaring it is the statement that
/// replay was considered, even though it is the one invoking <c>ICommandPipeline.Execute</c>.
/// </summary>
public class and_the_replay_handler_is_the_one_calling_execute : Specification
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
        public Task On(OrderPlaced @event) => Task.CompletedTask;

        [Replay]
        public Task OnReplay(OrderPlaced @event) =>
            commandPipeline.Execute(new ChargeCard(@event.OrderId));
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
