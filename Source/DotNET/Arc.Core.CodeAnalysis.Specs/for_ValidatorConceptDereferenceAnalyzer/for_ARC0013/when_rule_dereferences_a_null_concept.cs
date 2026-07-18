// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ValidatorConceptDereferenceAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ValidatorConceptDereferenceAnalyzer.for_ARC0013;

public class when_rule_dereferences_a_null_concept
{
    [Fact] async Task should_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using FluentValidation;
using Cratis.Concepts;

namespace TestNamespace
{
    public record OrderId(System.Guid Value) : ConceptAs<System.Guid>(Value);
    public record PlaceOrder(OrderId Id);

    public class PlaceOrderValidator : AbstractValidator<PlaceOrder>
    {
        public PlaceOrderValidator()
        {
            RuleFor(c => {|#0:c.Id.Value|}).NotEqual(System.Guid.Empty);
        }
    }
}",
        VerifyCS.Diagnostic("ARC0013")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Id"));
}
