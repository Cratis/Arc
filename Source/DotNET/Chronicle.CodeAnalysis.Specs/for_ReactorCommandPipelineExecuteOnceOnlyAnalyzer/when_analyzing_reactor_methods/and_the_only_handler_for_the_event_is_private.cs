// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorCommandPipelineExecuteOnceOnlyAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorCommandPipelineExecuteOnceOnlyAnalyzer.when_analyzing_reactor_methods;

/// <summary>
/// Chronicle's dispatch is not public-only: a private method whose first parameter is an event type is invoked
/// when no public candidate claims the same event, exactly as it would be here. Reporting only public handlers
/// would miss the case dispatch actually selects.
/// </summary>
public class and_the_only_handler_for_the_event_is_private : Specification
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
        Task On(BookReserved @event) =>
            {|#0:commandPipeline.Execute(new DecreaseStock(@event.Isbn))|};
    }
}",
                VerifyCS.Diagnostic("ARCCHR0006")
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments("On")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
