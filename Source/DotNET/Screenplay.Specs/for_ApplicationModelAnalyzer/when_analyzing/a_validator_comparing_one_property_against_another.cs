// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A rule comparing one property of a command against another names the second one with a lambda, which is the same
/// shape a rule chain names the property it is declared on. Reading only constants left every such rule looking like a
/// comparison that had been given nothing to compare against, and dropped the whole of the layer a domain is really
/// written in. An operand that is genuinely worked out while the request runs is a different thing and stays unread.
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

    ValidationRuleModel Rule(string property) => _rules.First(_ => _.Property == property);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_read_the_operand_as_the_property_it_names() => Rule("EndDate").Value.ShouldEqual(new PropertyPathSource("StartDate"));
    [Fact] void should_read_it_whatever_the_lambda_calls_its_parameter() => Rule("Deadline").Value.ShouldEqual(new PropertyPathSource("StartDate"));
    [Fact] void should_leave_an_operand_only_the_running_application_knows_unread() => Rule("StartDate").Value.ShouldBeNull();
    [Fact] void should_report_nothing_about_what_it_read() => _analysis.Diagnostics.ShouldBeEmpty();
}
