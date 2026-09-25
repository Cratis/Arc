// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ValidatorConceptDereferenceAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ValidatorConceptDereferenceAnalyzer.for_ARC0013;

public class when_selector_dereferences_a_static_concept_field
{
    [Fact] async Task should_not_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using FluentValidation;
using Cratis.Concepts;

namespace TestNamespace
{
    public record AuthorName(string Value) : ConceptAs<string>(Value);
    public record Author(AuthorName Name);
    public static class Defaults
    {
        public static readonly AuthorName Fallback = new(""Unknown"");
    }

    public class AuthorValidator : AbstractValidator<Author>
    {
        public AuthorValidator()
        {
            RuleFor(c => Defaults.Fallback.Value).NotEmpty();
        }
    }
}");
}
