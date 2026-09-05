// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.QueryParameterConceptTypeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_QueryParameterConceptTypeAnalyzer.for_ARC0015;

/// <summary>
/// The shape the rule exists to produce. Reporting it would make the rule unfixable and it would be suppressed
/// wholesale rather than adopted.
/// </summary>
public class when_the_parameter_is_already_declared_as_the_concept
{
    [Fact] async Task should_not_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Collections.Generic;
using Cratis.Concepts;
using Cratis.Arc.Queries.ModelBound;

namespace TestNamespace
{
    public record OrganizationNumber(string Value) : ConceptAs<string>(Value);

    [ReadModel]
    public record Customer(OrganizationNumber Number)
    {
        public static IEnumerable<Customer> ByOrgNumber(OrganizationNumber orgNumber) => Find(orgNumber);
        static IEnumerable<Customer> Find(OrganizationNumber number) => null;
    }
}");
}
