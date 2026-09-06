// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorCommandPipelineExecuteOnceOnlyAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorCommandPipelineExecuteOnceOnlyAnalyzer.when_analyzing_reactor_methods;

/// <summary>
/// Reporting is per execution call site, not per handler: two undecided handlers that share the same helper
/// produce one diagnostic at the shared <c>Execute</c> call, naming both.
/// </summary>
public class and_two_handlers_reach_the_same_helper : Specification
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
    [EventType]
    public record BookReturned(string Isbn);
    public record DecreaseStock(string Isbn);

    public class StockKeeping(ICommandPipeline commandPipeline) : IReactor
    {
        public Task BookReserved(BookReserved @event) => Adjust(@event.Isbn);

        public Task BookReturned(BookReturned @event) => Adjust(@event.Isbn);

        Task Adjust(string isbn) =>
            {|#0:commandPipeline.Execute(new DecreaseStock(isbn))|};
    }
}",
                VerifyCS.Diagnostic("ARCCHR0006")
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments("BookReserved", "BookReturned")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
