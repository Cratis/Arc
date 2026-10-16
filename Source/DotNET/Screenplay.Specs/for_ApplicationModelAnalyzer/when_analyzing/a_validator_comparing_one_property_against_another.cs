// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Date validation in ESM v26 admits literal operands only. Property-to-property rules and ambient clock reads
/// remain in code instead of becoming validation that the semantic compiler refuses.
/// </summary>
public class a_validator_comparing_one_property_against_another : Specification
{
    const string Source = """
        using System;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using FluentValidation;

        namespace Library.Lending.Reserving;

        [Command]
        public record ReserveBook(DateOnly StartDate, DateOnly EndDate, DateOnly Deadline)
        {
            public void Handle()
            {
            }
        }

        public class ReserveBookValidator : CommandValidator<ReserveBook>
        {
            public ReserveBookValidator()
            {
                RuleFor(_ => _.EndDate).GreaterThanOrEqualTo(_ => _.StartDate);
                RuleFor(_ => _.Deadline).LessThanOrEqualTo(command => command.StartDate);
                RuleFor(_ => _.StartDate).GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow));
            }
        }
        """;

    ApplicationModelAnalysis _analysis;
    IEnumerable<ValidationRuleModel> _rules;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _rules = _analysis.Slice().Commands.First().Validations;
    }

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_withhold_the_unadmitted_date_rules() => _rules.ShouldBeEmpty();
    [Fact] void should_report_both_property_operands() => _analysis.Diagnostics.Count(diagnostic => diagnostic.Message.Contains("require a literal", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_report_the_clock_operand() => _analysis.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("StartDate", StringComparison.Ordinal) && diagnostic.Message.Contains("clock", StringComparison.Ordinal)).ShouldBeTrue();
}
