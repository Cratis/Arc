// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.QueryParameterConceptTypeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_QueryParameterConceptTypeAnalyzer.for_ARC0015;

/// <summary>
/// One diagnostic per parameter, not one per method: each parameter is separately retypable and separately
/// unvalidated, so reporting on the method would leave the author guessing which one to fix.
/// </summary>
public class when_two_raw_parameters_are_both_converted
{
    [Fact] async Task should_report_one_diagnostic_per_parameter() => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Collections.Generic;
using Cratis.Concepts;
using Cratis.Arc.Queries.ModelBound;

namespace TestNamespace
{
    public record OrganizationNumber(string Value) : ConceptAs<string>(Value);
    public record CustomerName(string Value) : ConceptAs<string>(Value);

    [ReadModel]
    public record Customer(OrganizationNumber Number)
    {
        public static IEnumerable<Customer> By({|#0:string orgNumber|}, {|#1:string name|}) =>
            Find((OrganizationNumber)orgNumber, (CustomerName)name);
        static IEnumerable<Customer> Find(OrganizationNumber number, CustomerName name) => null;
    }
}",
        VerifyCS.Diagnostic("ARC0015")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("orgNumber", "string", "OrganizationNumber"),
        VerifyCS.Diagnostic("ARC0015")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(1)
            .WithArguments("name", "string", "CustomerName"));
}
