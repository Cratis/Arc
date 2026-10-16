// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// NotEqual is read only for Date and DateTime subjects, where decision 0087 admits it. On any other subject the rule
/// keeps the behavior it had before date comparisons were read.
/// </summary>
public class a_validator_with_a_not_equal_rule_on_a_number : Specification
{
    const string Source = """
        using System;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using FluentValidation;

        namespace Library.Lending.Reserving;

        [Command]
        public record ReserveBook(int Copies, DateOnly Due)
        {
            public void Handle()
            {
            }
        }

        public class ReserveBookValidator : CommandValidator<ReserveBook>
        {
            public ReserveBookValidator()
            {
                RuleFor(_ => _.Copies).NotEqual(0);
                RuleFor(_ => _.Due).NotEqual(new DateOnly(2026, 1, 1));
            }
        }
        """;

    IEnumerable<ValidationRuleModel> _rules;

    void Establish() => _rules = Analyzed.Source(Source).Slice().Commands.First().Validations;

    [Fact] void should_not_read_it_for_a_number() => _rules.Any(rule => rule.Property == "Copies").ShouldBeFalse();
    [Fact] void should_read_it_for_a_date() => _rules.Single(rule => rule.Property == "Due").Kind.ShouldEqual(ValidationRuleKind.NotEqual);
}
