// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ReadModelAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ReadModelAnalyzer.when_record_readmodel_has_incorrect_query_signature;

public class returning_different_type : Specification
{
    Task _result;

    void Because() => _result = VerifyCS.VerifyAnalyzerAsync(@"
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
            .WithArguments("GetOther", "TestReadModel", "TestNamespace.OtherType"));

    [Fact] void should_report_diagnostic() => _result.Wait();
}
