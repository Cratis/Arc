// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReactorCommandPipelineExecuteOnceOnlyAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_ReactorCommandPipelineExecuteOnceOnlyAnalyzer.when_analyzing_reactor_methods;

public class and_non_reactor_invokes_execute : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Cratis.Arc.Commands;

namespace TestNamespace
{
    public record DecreaseStock(string Isbn);

    public class StockKeeping(ICommandPipeline commandPipeline)
    {
        public Task Decrease(string isbn) =>
            commandPipeline.Execute(new DecreaseStock(isbn));
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
