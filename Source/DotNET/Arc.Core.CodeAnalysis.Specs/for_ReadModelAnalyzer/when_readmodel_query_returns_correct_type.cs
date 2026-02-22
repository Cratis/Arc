// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ReadModelAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ReadModelAnalyzer;

public class when_readmodel_query_returns_correct_type
{
    [Fact] async Task should_not_report_diagnostic_for_readmodel_return_type() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;
using System.Collections.Generic;

namespace TestNamespace
{
    [ReadModel]
    public class TestReadModel
    {
        public static TestReadModel GetById(int id) => new();
    }
}");

    [Fact] async Task should_not_report_diagnostic_for_collection_return_type() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;
using System.Collections.Generic;

namespace TestNamespace
{
    [ReadModel]
    public class TestReadModel
    {
        public static IEnumerable<TestReadModel> GetAll() => [];
    }
}");

    [Fact] async Task should_not_report_diagnostic_for_task_return_type() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;
using System.Threading.Tasks;

namespace TestNamespace
{
    [ReadModel]
    public class TestReadModel
    {
        public static Task<TestReadModel> GetByIdAsync(int id) => Task.FromResult(new TestReadModel());
    }
}");
}
