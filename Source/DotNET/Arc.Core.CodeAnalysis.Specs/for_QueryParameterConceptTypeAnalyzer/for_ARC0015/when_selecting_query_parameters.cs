// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.QueryParameterConceptTypeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_QueryParameterConceptTypeAnalyzer.for_ARC0015;

#pragma warning disable MA0136 // Compiler fixture behavior is independent of line endings.

public class when_selecting_query_parameters
{
    [Theory]
    [InlineData(true, "", false)]
    [InlineData(true, "[Query]", true)]
    [InlineData(false, "", true)]
    [InlineData(false, "[Query]", true)]
    public async Task should_report_only_for_selected_queries(bool explicitMode, string attribute, bool expected)
    {
        var source = $$"""
            using System.Threading.Tasks;
            using Cratis.Arc.Queries.ModelBound;
            using Cratis.Concepts;
            namespace TestApp;
            public record OrderId(string Value) : ConceptAs<string>(Value);
            [ReadModel(RequireQueryAttribute = {{explicitMode.ToString().ToLowerInvariant()}})]
            public record Order(OrderId Id)
            {
                {{attribute}}
                public static Order Create({|#0:string id|}) => new((OrderId)id);
            }
            """;
        ExpectedDiagnostic[] diagnostics = expected
            ? [VerifyCS.Diagnostic("ARC0015").WithSeverity(DiagnosticSeverity.Warning).WithLocation(0).WithArguments("id", "string", "OrderId")]
            : [];
        await VerifyCS.VerifyAnalyzerAsync(source, diagnostics);
    }

    [Theory]
    [InlineData("BeginReplay")]
    [InlineData("EndReplay")]
    [InlineData("BeginReplayPartition")]
    [InlineData("EndReplayPartition")]
    public async Task should_not_analyze_unmarked_task_helper_parameters(string name) => await VerifyCS.VerifyAnalyzerAsync($$"""
        using System.Threading.Tasks;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Concepts;
        namespace TestApp;
        public record OrderId(string Value) : ConceptAs<string>(Value);
        [ReadModel(RequireQueryAttribute = true)]
        public record Order
        {
            public static Task {{name}}(string partition, object service)
            {
                OrderId id = (OrderId)partition;
                return Task.CompletedTask;
            }
        }
        """);
}
