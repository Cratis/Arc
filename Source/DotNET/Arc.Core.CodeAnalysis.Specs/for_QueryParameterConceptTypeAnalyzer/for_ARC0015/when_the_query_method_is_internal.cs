// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.QueryParameterConceptTypeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_QueryParameterConceptTypeAnalyzer.for_ARC0015;

/// <summary>
/// Internal methods are not query endpoints, so their parameters are not query inputs.
/// </summary>
public class when_the_query_method_is_internal
{
    [Fact] async Task should_not_analyze_parameters_of_an_internal_helper() => await VerifyCS.VerifyAnalyzerAsync(@"
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
}");
}
