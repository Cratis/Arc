// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ValidatorConceptDereferenceAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ValidatorConceptDereferenceAnalyzer.for_ARC0013;

public class when_selector_dereferences_a_nested_concept
{
    [Fact] async Task should_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using FluentValidation;
using Cratis.Concepts;

namespace TestNamespace
{
    public record AuthorName(string Value) : ConceptAs<string>(Value);
    public record Author(AuthorName Name);
    public record Submission(Author Inner);

    public class SubmissionValidator : AbstractValidator<Submission>
    {
        public SubmissionValidator()
        {
            RuleFor(c => {|#0:c.Inner.Name.Value|}).NotEmpty();
        }
    }
}",
        VerifyCS.Diagnostic("ARC0013")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Name"));
}
