// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorCommandPipelineExecuteOnceOnlyAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorCommandPipelineExecuteOnceOnlyAnalyzer.when_analyzing_reactor_methods;

/// <summary>
/// A call made from inside a lambda still has to be attributed to the handler that declares it — climbing
/// straight to the containing symbol would stop at the compiler-synthesized lambda method instead.
/// </summary>
public class and_the_execute_is_in_a_lambda_inside_a_handler : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
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
        public Task On(BookReserved @event)
        {
            Action act = () => {|#0:commandPipeline.Execute(new DecreaseStock(@event.Isbn))|};
            act();
            return Task.CompletedTask;
        }
    }
}",
                VerifyCS.Diagnostic("ARCCHR0006")
                    .WithSeverity(DiagnosticSeverity.Warning)
                    .WithLocation(0)
                    .WithArguments("On")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
