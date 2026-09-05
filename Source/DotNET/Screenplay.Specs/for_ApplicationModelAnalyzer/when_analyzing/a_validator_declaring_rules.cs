// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// Reading the constructor recovers what the runtime rule descriptor loses - which rules were declared for each
/// element of a collection, and which rules live in code and can only be reported.
/// </summary>
public class a_validator_declaring_rules : Specification
{
    const string Source = """
        using System.Collections.Generic;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using FluentValidation;

        namespace Library.Authors.Registration;

        [Command]
        public record RegisterAuthor(string Name, string Email, IEnumerable<int> Ratings)
        {
            public void Handle()
            {
            }
        }

        public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
        {
            public RegisterAuthorValidator()
            {
                RuleFor(_ => _.Name).NotEmpty().WithMessage("An author must have a name");
                RuleFor(_ => _.Name).MaximumLength(100);
                RuleFor(_ => _.Email).EmailAddress();
                RuleForEach(_ => _.Ratings).GreaterThan(0);
                RuleFor(_ => _.Name).Must(name => name.Length > 2);
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

    ValidationRuleModel Rule(ValidationRuleKind kind) => _rules.First(_ => _.Kind == kind);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_recover_every_declarative_rule() => _rules.Select(_ => _.Kind).ShouldContainOnly([ValidationRuleKind.NotEmpty, ValidationRuleKind.Max, ValidationRuleKind.Matches, ValidationRuleKind.AllGreaterThan]);
    [Fact] void should_name_the_property_each_rule_applies_to() => Rule(ValidationRuleKind.NotEmpty).Property.ShouldEqual("Name");
    [Fact] void should_carry_the_message_written_after_a_rule() => Rule(ValidationRuleKind.NotEmpty).Message.ShouldEqual("An author must have a name");
    [Fact] void should_carry_the_operand_of_a_rule_that_takes_one() => Rule(ValidationRuleKind.Max).Value.ShouldEqual(100);
    [Fact] void should_express_an_email_rule_as_the_pattern_the_grammar_knows() => Rule(ValidationRuleKind.Matches).Value.ShouldEqual("email");
    [Fact] void should_tell_a_rule_on_each_element_from_one_on_the_collection() => Rule(ValidationRuleKind.AllGreaterThan).Property.ShouldEqual("Ratings");
    [Fact] void should_report_the_rule_that_lives_in_code() => _analysis.Diagnostics.Select(_ => _.Code).ShouldContainOnly([ScreenplayDiagnosticCodes.UnmappableValidationRule]);
    [Fact] void should_say_which_rule_it_was() => _analysis.Diagnostics.Single().Message.Contains("'Must'", StringComparison.Ordinal).ShouldBeTrue();
}
