// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ReadModelAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ReadModelAnalyzer.when_validating_incorrect_query_method_signature;

public class and_query_returns_different_type : Specification
{
    Exception result;

    async Task Because()
    {
        try
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
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
        }
        catch (Exception ex)
        {
            result = ex;
        }
    }

    [Fact] void should_report_diagnostic() => result.ShouldBeNull();
}
