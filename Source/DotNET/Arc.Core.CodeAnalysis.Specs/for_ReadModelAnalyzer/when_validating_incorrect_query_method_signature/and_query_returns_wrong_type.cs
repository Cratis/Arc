// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ReadModelAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ReadModelAnalyzer.when_validating_incorrect_query_method_signature;

public class and_query_returns_wrong_type : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;

namespace TestNamespace
{
    [ReadModel]
    public record TestReadModel(int Id, string Name)
    {
        public static {|#0:string GetName(int id)|} => ""name"";
    }
}",
                VerifyCS.Diagnostic("ARC0001")
                    .WithLocation(0)
                    .WithArguments("GetName", "TestReadModel", "string")));

    [Fact] void should_report_diagnostic() => _result.ShouldBeNull();
}
