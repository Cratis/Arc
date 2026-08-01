// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// A length range is one rule to the developer and two to the document - a lower bound and an upper bound - so the
/// message written after it was written about both. Attaching it to the upper bound alone would leave the lower bound
/// reporting a message nobody wrote, which is the kind of wrong a reader cannot see.
/// </summary>
public class a_validator_declaring_a_length_range : Specification
{
    const string Source = """
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using FluentValidation;

        namespace Library.Authors.Registration;

        [Command]
        public record RegisterAuthor(string Name, string Pen)
        {
            public void Handle()
            {
            }
        }

        public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
        {
            public RegisterAuthorValidator()
            {
                RuleFor(_ => _.Name).Length(2, 100).WithMessage("A name is between 2 and 100 characters");
                RuleFor(_ => _.Pen).NotEmpty().WithMessage("A pen name is required").MaximumLength(20);
            }
        }
        """;

    const string RangeMessage = "A name is between 2 and 100 characters";

    ApplicationModelAnalysis _analysis;
    IEnumerable<ValidationRuleModel> _rules;

    void Establish()
    {
        _analysis = Analyzed.Source(Source);
        _rules = _analysis.Slice().Commands.First().Validations;
    }

    ValidationRuleModel Rule(string property, ValidationRuleKind kind) =>
        _rules.First(_ => _.Property == property && _.Kind == kind);

    [Fact] void should_compile_the_source_it_analyzed() => Analyzed.ErrorsIn(("Library/Feature/Slice/Slice.cs", Source)).ShouldBeEmpty();
    [Fact] void should_split_the_range_into_a_lower_and_an_upper_bound() => _rules.Where(_ => _.Property == "Name").Select(_ => _.Kind).ShouldContainOnly([ValidationRuleKind.Min, ValidationRuleKind.Max]);
    [Fact] void should_carry_the_lower_bound() => Rule("Name", ValidationRuleKind.Min).Value.ShouldEqual(2);
    [Fact] void should_carry_the_upper_bound() => Rule("Name", ValidationRuleKind.Max).Value.ShouldEqual(100);
    [Fact] void should_carry_the_message_on_the_lower_bound() => Rule("Name", ValidationRuleKind.Min).Message.ShouldEqual(RangeMessage);
    [Fact] void should_carry_the_message_on_the_upper_bound() => Rule("Name", ValidationRuleKind.Max).Message.ShouldEqual(RangeMessage);
    [Fact] void should_carry_a_message_on_the_single_rule_it_was_written_after() => Rule("Pen", ValidationRuleKind.NotEmpty).Message.ShouldEqual("A pen name is required");
    [Fact] void should_leave_a_later_rule_without_a_message_of_its_own() => Rule("Pen", ValidationRuleKind.Max).Message.ShouldBeNull();
    [Fact] void should_report_nothing() => _analysis.Diagnostics.ShouldBeEmpty();
}
