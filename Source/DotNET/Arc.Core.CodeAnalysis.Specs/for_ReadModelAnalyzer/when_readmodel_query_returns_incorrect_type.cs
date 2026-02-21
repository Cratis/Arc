// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ReadModelAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ReadModelAnalyzer;

public class when_readmodel_query_returns_incorrect_type
{
    [Fact] async Task should_report_diagnostic_for_wrong_return_type() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;

namespace TestNamespace
{
    [ReadModel]
    public class TestReadModel
    {
        public static {|#0:string GetName(int id)|} => ""name"";
    }
}",
        VerifyCS.Diagnostic("ARC0001")
            .WithLocation(0)
            .WithArguments("GetName", "TestReadModel", "string"));
}
