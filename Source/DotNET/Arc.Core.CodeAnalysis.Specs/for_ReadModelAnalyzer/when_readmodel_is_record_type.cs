// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ReadModelAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ReadModelAnalyzer;

public class when_readmodel_is_record_type
{
    [Fact] void should_not_report_diagnostic_for_record_with_properties() => VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;
using System.Collections.Generic;

namespace TestNamespace
{
    [ReadModel]
    public record TestReadModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        public static TestReadModel GetById(int id) => new();
    }
}").Wait();

    [Fact] void should_not_report_diagnostic_for_record_with_primary_constructor() => VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;
using System.Collections.Generic;

namespace TestNamespace
{
    [ReadModel]
    public record TestReadModel(int Id, string Name)
    {
        public static TestReadModel GetById(int id) => new(id, ""test"");
    }
}").Wait();

    [Fact] void should_not_report_diagnostic_for_record_returning_collection() => VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;
using System.Collections.Generic;

namespace TestNamespace
{
    [ReadModel]
    public record TestReadModel(int Id, string Name)
    {
        public static IEnumerable<TestReadModel> GetAll() => [];
    }
}").Wait();

    [Fact] void should_not_report_diagnostic_for_record_with_task_return() => VerifyCS.VerifyAnalyzerAsync(@"
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
}").Wait();

    [Fact] void should_not_report_diagnostic_for_record_with_task_collection_return() => VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TestNamespace
{
    [ReadModel]
    public record TestReadModel(int Id, string Name)
    {
        public static Task<IEnumerable<TestReadModel>> GetAllAsync() => Task.FromResult<IEnumerable<TestReadModel>>([]);
    }
}").Wait();

    [Fact] void should_not_report_diagnostic_for_record_struct() => VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;
using System.Collections.Generic;

namespace TestNamespace
{
    [ReadModel]
    public record struct TestReadModel(int Id, string Name)
    {
        public static TestReadModel GetById(int id) => new(id, ""test"");
    }
}").Wait();

    [Fact] void should_not_report_diagnostic_for_record_with_mixed_properties() => VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;
using System.Collections.Generic;

namespace TestNamespace
{
    [ReadModel]
    public record TestReadModel(int Id, string Name)
    {
        public string Email { get; set; }
        
        public static TestReadModel GetById(int id) => new(id, ""test"") { Email = ""test@test.com"" };
    }
}").Wait();
}
