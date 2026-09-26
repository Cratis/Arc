// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorCommandPipelineExecuteOnceOnlyAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorCommandPipelineExecuteOnceOnlyAnalyzer.when_replay_is_handled_deliberately;

/// <summary>
/// A base replay method displaced by an override is not an additional replay decision at runtime.
/// </summary>
public class and_a_base_replay_handler_is_overridden_without_replay : Specification
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
        public virtual Task OnReplay(OrderPlaced @event) => Task.CompletedTask;
    }

    public class OrderProcessing(ICommandPipeline commandPipeline) : ReplayHandlers, IReactor
    {
        public override Task OnReplay(OrderPlaced @event) => Task.CompletedTask;

        public Task On(OrderPlaced @event) =>
            {|#0:commandPipeline.Execute(new ChargeCard(@event.OrderId))|};
    }
}",
                VerifyCS.Diagnostic("ARCCHR0006")
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments("'On' invokes")));

    [Fact] void should_report_only_for_the_live_handler() => _result.ShouldBeNull();
}
