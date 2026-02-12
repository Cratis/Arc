// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ReadModelAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ReadModelAnalyzer;

public class when_readmodel_query_returns_correct_type
{
    [Fact] void should_not_report_diagnostic_for_readmodel_return_type() => VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;
using System.Collections.Generic;

namespace TestNamespace
{
    [ReadModel]
    public class TestReadModel
    {
        public static TestReadModel GetById(int id) => new();
    }
}").Wait();

    [Fact] void should_not_report_diagnostic_for_collection_return_type() => VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;
using System.Collections.Generic;

namespace TestNamespace
{
    [ReadModel]
    public class TestReadModel
    {
        public static IEnumerable<TestReadModel> GetAll() => [];
    }
}").Wait();

    [Fact] void should_not_report_diagnostic_for_task_return_type() => VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;
using System.Threading.Tasks;

namespace TestNamespace
{
    [ReadModel]
    public class TestReadModel
    {
        public static Task<TestReadModel> GetByIdAsync(int id) => Task.FromResult(new TestReadModel());
    }
}").Wait();
}
