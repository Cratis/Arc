// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ValidatorConceptDereferenceAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ValidatorConceptDereferenceAnalyzer.for_ARC0013;

public class when_selector_is_the_concept_root_value
{
    [Fact] async Task should_not_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using FluentValidation;
using Cratis.Concepts;

namespace TestNamespace
{
    public record OrderId(System.Guid Value) : ConceptAs<System.Guid>(Value);

    public class OrderIdValidator : AbstractValidator<OrderId>
    {
        public OrderIdValidator()
        {
            RuleFor(x => x.Value).NotEqual(System.Guid.Empty);
        }
    }
}");
}
