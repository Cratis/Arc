// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorCommandPipelineExecuteOnceOnlyAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorCommandPipelineExecuteOnceOnlyAnalyzer.when_analyzing_reactor_methods;

/// <summary>
/// A private method whose first parameter is the same event type a public handler already claims is never a
/// dispatch target — Chronicle would only ever invoke the public one for that event. The private method's own
/// <c>Execute</c> call is dead code from a dispatch perspective, and is left unreported.
/// </summary>
public class and_a_private_helper_takes_the_event_a_public_handler_claims : Specification
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

    public class StockKeeping(ICommandPipeline commandPipeline) : IReactor
    {
        public Task On(BookReserved @event) => Task.CompletedTask;

        Task LegacyOn(BookReserved @event) =>
            commandPipeline.Execute(new DecreaseStock(@event.Isbn));
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
