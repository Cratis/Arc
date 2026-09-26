// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ValidatorConceptDereferenceAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ValidatorConceptDereferenceAnalyzer.for_ARC0013;

public class when_selector_dereferences_a_static_member_of_a_concept
{
    [Fact] async Task should_not_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using FluentValidation;
using Cratis.Concepts;

namespace TestNamespace
{
    public record AuthorName(string Value) : ConceptAs<string>(Value)
    {
        public static readonly AuthorName NotSet = new(""Unknown"");
    }
    public record Author(AuthorName Name);

    public class AuthorValidator : AbstractValidator<Author>
    {
        public AuthorValidator()
        {
            RuleFor(c => AuthorName.NotSet.Value).NotEmpty();
        }
    }
}");
}
