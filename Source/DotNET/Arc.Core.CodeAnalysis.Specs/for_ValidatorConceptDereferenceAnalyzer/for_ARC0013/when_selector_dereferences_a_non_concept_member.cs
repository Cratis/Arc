// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ValidatorConceptDereferenceAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ValidatorConceptDereferenceAnalyzer.for_ARC0013;

public class when_selector_dereferences_a_non_concept_member
{
    [Fact] async Task should_not_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using FluentValidation;

namespace TestNamespace
{
    public record PlaceOrder(string Name);

    public class PlaceOrderValidator : AbstractValidator<PlaceOrder>
    {
        public PlaceOrderValidator()
        {
            RuleFor(c => c.Name.Length).GreaterThan(0);
        }
    }
}");
}
