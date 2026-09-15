// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorCommandPipelineExecuteOnceOnlyAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorCommandPipelineExecuteOnceOnlyAnalyzer.when_analyzing_reactor_methods;

/// <summary>
/// A class-level [OnceOnly] excludes the whole reactor from replay, so no handler on it can ever need a replay
/// decision.
/// </summary>
public class and_the_reactor_is_marked_once_only : Specification
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
    public record BookReserved(string Isbn);
    public record DecreaseStock(string Isbn);

    [OnceOnly]
    public class StockKeeping(ICommandPipeline commandPipeline) : IReactor
    {
        public Task On(BookReserved @event) =>
            commandPipeline.Execute(new DecreaseStock(@event.Isbn));
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
