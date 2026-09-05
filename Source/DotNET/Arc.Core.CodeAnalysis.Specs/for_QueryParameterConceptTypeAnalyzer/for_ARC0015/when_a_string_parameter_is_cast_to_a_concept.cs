// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.QueryParameterConceptTypeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_QueryParameterConceptTypeAnalyzer.for_ARC0015;

public class when_a_string_parameter_is_cast_to_a_concept
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
        public static IEnumerable<Customer> ByOrgNumber({|#0:string orgNumber|}) => Find((OrganizationNumber)orgNumber);
        static IEnumerable<Customer> Find(OrganizationNumber number) => null;
    }
}",
        VerifyCS.Diagnostic("ARC0015")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("orgNumber", "string", "OrganizationNumber"));
}
