// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using ModelRuleKind = Cratis.Arc.Screenplay.Model.ValidationRuleKind;
using SyntaxRuleKind = Cratis.Screenplay.Syntax.ValidationRuleKind;

namespace Cratis.Arc.Screenplay.for_ValidationSyntaxBuilder.when_building;

/// <summary>
/// A message starting with the localization prefix is a key into the companion strings file and is written
/// unquoted, while a named pattern such as <c>email</c> is written as a bare word rather than as a string.
/// </summary>
public class rules_carrying_operands_and_messages : given.a_validation_syntax_builder
{
    IEnumerable<ValidationRuleSyntax> _rules;

    void Because() => _rules = _builder
        .Build(
            [
                new("Name", ModelRuleKind.NotEmpty, null, "A name is required"),
                new("Name", ModelRuleKind.Max, 100, "$strings.authors.nameTooLong"),
                new("Contact.Email", ModelRuleKind.Matches, "email", null),
                new("Reference", ModelRuleKind.Matches, @"^INV-\d+$", null)
            ],
            "Library.Authors.Registration")
        .OfType<DeclarativeValidateSyntax>()
        .SelectMany(_ => _.Rules);

    [Fact] void should_build_every_rule() => _rules.Count().ShouldEqual(4);
    [Fact] void should_camel_case_a_nested_property_path() => _rules.ElementAt(2).Property.ShouldEqual("contact.email");
    [Fact] void should_leave_a_rule_without_an_operand_without_one() => _rules.ElementAt(0).Value.ShouldBeNull();
    [Fact] void should_convert_a_numeric_operand_to_a_double() => ((LiteralExpressionSyntax)_rules.ElementAt(1).Value!).Value.ShouldBeOfExactType<double>();
    [Fact] void should_keep_a_localization_key_unquoted() => _rules.ElementAt(1).Message.ShouldEqual("$strings.authors.nameTooLong");
    [Fact] void should_write_a_named_pattern_as_a_bare_word() => _rules.ElementAt(2).Value.ShouldBeOfExactType<PathExpressionSyntax>();
    [Fact] void should_write_a_regular_expression_as_a_literal() => _rules.ElementAt(3).Value.ShouldBeOfExactType<LiteralExpressionSyntax>();
    [Fact] void should_map_the_kind_of_every_rule() => _rules.Select(_ => _.Rule).ShouldEqual([SyntaxRuleKind.NotEmpty, SyntaxRuleKind.Max, SyntaxRuleKind.Matches, SyntaxRuleKind.Matches]);
    [Fact] void should_report_nothing() => _diagnostics.All.ShouldBeEmpty();
}
