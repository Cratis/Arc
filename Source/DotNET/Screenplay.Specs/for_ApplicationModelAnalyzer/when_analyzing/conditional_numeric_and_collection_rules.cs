// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

public class conditional_numeric_and_collection_rules : Specification
{
    const string Source = """
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using FluentValidation;
        namespace Library.Ordering.Placing;
        [Command] public record PlaceOrder(int Quantity, int[] Amounts)
        {
            public void Handle() { }
        }
        public class PlaceOrderValidator : CommandValidator<PlaceOrder>
        {
            public PlaceOrderValidator()
            {
                RuleFor(c => c.Quantity).GreaterThan(0);
                RuleSet("draft", () =>
                {
                    RuleFor(c => c.Quantity).GreaterThan(10).WithMessage("Draft");
                    RuleForEach(c => c.Amounts).GreaterThanOrEqualTo(1);
                });
                RuleForEach(c => c.Amounts).GreaterThan(0).When(c => c.Quantity > 1);
            }
        }
        """;

    ApplicationModelAnalysis _analysis;

    void Because() => _analysis = Analyzed.Source(Source);

    [Fact] void should_compile_the_source() => Analyzed.ErrorsIn((Analyzed.SlicePath, Source)).ShouldBeEmpty();
    [Fact] void should_keep_only_the_unconditional_numeric_rule() => _analysis.Slice().Commands.Single().Validations.Count().ShouldEqual(1);
    [Fact] void should_keep_its_operand() => _analysis.Slice().Commands.Single().Validations.Single().Value.ShouldEqual(0);
    [Fact] void should_not_reattach_an_omitted_message() => _analysis.Slice().Commands.Single().Validations.Single().Message.ShouldBeNull();
    [Fact] void should_name_the_rule_set_for_the_collection_rule() => _analysis.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableValidationRule && diagnostic.Message.Contains("GreaterThanOrEqualTo", StringComparison.Ordinal) && diagnostic.Message.Contains("RuleSet(\"draft\")", StringComparison.Ordinal)).ShouldBeTrue();
}
