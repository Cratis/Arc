// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

/// <summary>
/// The client-side <c>matches</c> rule takes a regular expression, not a string, so the pattern is carried as a
/// <see cref="RegularExpressionPattern"/> the formatter can emit as a regex literal.
/// </summary>
public class when_extracting_a_regex_rule : Specification
{
    ValidationRuleDescriptor _rule;

    void Because() => _rule = ValidationRulesExtractor.ExtractValidationRules(
            typeof(TestCommandWithRegex).Assembly,
            typeof(TestCommandWithRegex))
        .Single(_ => _.PropertyName == "postalCode").Rules.Single();

    [Fact] void should_be_the_matches_rule() => _rule.RuleName.ShouldEqual("matches");
    [Fact] void should_carry_the_pattern_as_a_regular_expression() => _rule.Arguments.Single().ShouldBeOfExactType<RegularExpressionPattern>();
    [Fact] void should_preserve_the_pattern() => ((RegularExpressionPattern)_rule.Arguments.Single()).Pattern.ShouldEqual(@"^\d{4}$");
}
