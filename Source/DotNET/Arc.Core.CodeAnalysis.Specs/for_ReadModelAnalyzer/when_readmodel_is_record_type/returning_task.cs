// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ReadModelAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ReadModelAnalyzer.when_readmodel_is_record_type;

public class returning_task : Specification
{
    Task _result;

    void Because() => _result = VerifyCS.VerifyAnalyzerAsync(@"
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
}");

    [Fact] void should_not_report_diagnostic() => _result.Wait();
}
