// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.QueryParameterConceptTypeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_QueryParameterConceptTypeAnalyzer.for_ARC0015;

public class when_a_creator_converts_a_raw_parameter
{
    [Theory]
    [InlineData("[NotAQuery]", "", false)]
    [InlineData("", ", EventContext context", false)]
    [InlineData("", "", true)]
    public async Task should_report_only_for_a_real_query(string attribute, string context, bool expected)
    {
#pragma warning disable MA0136 // Compiler fixture behavior is independent of line endings.
        var source = $$"""
            using Cratis.Arc.Queries.ModelBound;
            using Cratis.Concepts;
            namespace Cratis.Chronicle.Events { public struct EventContext { } }
            namespace TestApp
            {
                using Cratis.Chronicle.Events;
                public record OrderId(string Value) : ConceptAs<string>(Value);
                [ReadModel]
                public record Order(OrderId Id)
                {
                    {{attribute}}
                    public static Order Create({|#0:string id|}{{context}}) => new((OrderId)id);
                }
            }
            """;
#pragma warning restore MA0136
        ExpectedDiagnostic[] diagnostics = expected
            ? [VerifyCS.Diagnostic("ARC0015").WithSeverity(DiagnosticSeverity.Warning).WithLocation(0).WithArguments("id", "string", "OrderId")]
            : [];
        await VerifyCS.VerifyAnalyzerAsync(source, diagnostics);
    }
}
