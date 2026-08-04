// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.QueryParameterConceptTypeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_QueryParameterConceptTypeAnalyzer.for_ARC0015;

/// <summary>
/// Query discovery registers internal methods too, so an internal query is just as routable - and just as
/// unvalidated - as a public one. Restricting the rule to public methods would leave the same defect unreported
/// wherever a read model keeps its queries internal.
/// </summary>
public class when_the_query_method_is_internal
{
    [Fact] async Task should_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Collections.Generic;
using Cratis.Concepts;
using Cratis.Arc.Queries.ModelBound;

namespace TestNamespace
{
    public record OrganizationNumber(string Value) : ConceptAs<string>(Value);

    [ReadModel]
    public record Customer(OrganizationNumber Number)
    {
        internal static IEnumerable<Customer> ByOrgNumber({|#0:string orgNumber|}) => Find((OrganizationNumber)orgNumber);
        static IEnumerable<Customer> Find(OrganizationNumber number) => null;
    }
}",
        VerifyCS.Diagnostic("ARC0015")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("orgNumber", "string", "OrganizationNumber"));
}
