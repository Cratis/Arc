// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ReadModelAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ReadModelAnalyzer;

public class when_record_readmodel_has_incorrect_query_signature
{
    [Fact] void should_report_diagnostic_for_record_with_wrong_return_type() => VerifyCS.VerifyAnalyzerAsync(@"
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
            .WithArguments("GetName", "TestReadModel", "string")).Wait();

    [Fact] void should_report_diagnostic_for_record_returning_different_type() => VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Queries.ModelBound;

namespace TestNamespace
{
    public record OtherType(int Value);
    
    [ReadModel]
    public record TestReadModel(int Id, string Name)
    {
        public static {|#0:OtherType GetOther(int id)|} => new(42);
    }
}",
        VerifyCS.Diagnostic("ARC0001")
            .WithLocation(0)
            .WithArguments("GetOther", "TestReadModel", "TestNamespace.OtherType")).Wait();
}
