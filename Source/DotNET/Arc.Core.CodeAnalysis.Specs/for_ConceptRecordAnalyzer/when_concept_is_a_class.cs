// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ConceptRecordAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ConceptRecordAnalyzer;

public class when_concept_is_a_class
{
    [Fact] async Task should_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Concepts;

namespace TestNamespace
{
    public class {|#0:AuthorId|} : ConceptAs<System.Guid>
    {
    }
}",
        VerifyCS.Diagnostic("ARC0009")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("AuthorId"));
}
