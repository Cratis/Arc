// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.QueryParameterConceptTypeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_QueryParameterConceptTypeAnalyzer.for_ARC0015;

/// <summary>
/// The missing validator is a consequence of Arc binding the argument, so a static method nothing routes to has no
/// validator to lose. An ordinary helper that converts a string to a concept is just code.
/// </summary>
public class when_the_type_is_not_a_read_model
{
    [Fact] async Task should_not_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Collections.Generic;
using Cratis.Concepts;

namespace TestNamespace
{
    public record OrganizationNumber(string Value) : ConceptAs<string>(Value);

    public static class Helpers
    {
        public static IEnumerable<string> ByOrgNumber(string orgNumber) => Find((OrganizationNumber)orgNumber);
        static IEnumerable<string> Find(OrganizationNumber number) => null;
    }
}");
}
