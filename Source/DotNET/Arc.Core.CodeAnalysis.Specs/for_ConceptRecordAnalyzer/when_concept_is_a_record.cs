// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ConceptRecordAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ConceptRecordAnalyzer;

public class when_concept_is_a_record
{
    [Fact] async Task should_not_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Concepts;

namespace TestNamespace
{
    public record AuthorId(System.Guid Value) : ConceptAs<System.Guid>(Value);
}");
}
