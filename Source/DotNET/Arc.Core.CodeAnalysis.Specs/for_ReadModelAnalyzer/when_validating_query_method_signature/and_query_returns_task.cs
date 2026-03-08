// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ReadModelAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ReadModelAnalyzer.when_validating_query_method_signature;

public class and_query_returns_task : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TestNamespace
{
    [ReadModel]
    public record TestReadModel(int Id, string Name)
    {
        public static Task<TestReadModel> GetByIdAsync(int id) => Task.FromResult(new TestReadModel(id, ""test""));
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
