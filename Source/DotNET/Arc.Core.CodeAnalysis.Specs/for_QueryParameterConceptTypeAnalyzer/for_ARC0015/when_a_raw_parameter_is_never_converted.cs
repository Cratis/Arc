// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.QueryParameterConceptTypeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_QueryParameterConceptTypeAnalyzer.for_ARC0015;

/// <summary>
/// A raw parameter is a legal query signature, and a consumer who prefers them has no missing validator to be told
/// about. The conversion is the whole signal: it is what says the author wanted the concept.
/// </summary>
public class when_a_raw_parameter_is_never_converted
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
        public static IEnumerable<Customer> Search(string term) => Find(term);
        static IEnumerable<Customer> Find(string term) => null;
    }
}");
}
