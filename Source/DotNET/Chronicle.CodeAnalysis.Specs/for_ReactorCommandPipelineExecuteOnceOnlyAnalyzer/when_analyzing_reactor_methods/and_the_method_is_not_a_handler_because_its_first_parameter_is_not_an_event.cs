// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorCommandPipelineExecuteOnceOnlyAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorCommandPipelineExecuteOnceOnlyAnalyzer.when_analyzing_reactor_methods;

public class and_the_method_is_not_a_handler_because_its_first_parameter_is_not_an_event : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Cratis.Chronicle.Reactors;
using Cratis.Arc.Commands;

namespace TestNamespace
{
    public record NotAnEvent(string Isbn);
    public record DecreaseStock(string Isbn);

    public class StockKeeping(ICommandPipeline commandPipeline) : IReactor
    {
        Task On(NotAnEvent value) =>
            commandPipeline.Execute(new DecreaseStock(value.Isbn));
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
