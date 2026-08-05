// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.QueryParameterConceptTypeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_QueryParameterConceptTypeAnalyzer.for_ARC0015;

/// <summary>
/// Only a conversion to a concept means a validator was skipped. Converting for any other reason is ordinary code
/// and reporting it would make the rule noise.
/// </summary>
public class when_a_raw_parameter_is_converted_to_a_non_concept
{
    [Fact] async Task should_not_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Collections.Generic;
using Cratis.Concepts;
using Cratis.Arc.Queries.ModelBound;

namespace TestNamespace
{
    public record OrganizationNumber(string Value) : ConceptAs<string>(Value);

    [ReadModel]
    public record Customer(OrganizationNumber Number)
    {
        public static IEnumerable<Customer> Since(string on) => Find(DateTime.Parse(on));
        static IEnumerable<Customer> Find(DateTime on) => null;
    }
}");
}
