// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_extracting_fluent_validation_rules : Specification
{
    IDictionary<string, PropertyValidationDescriptor> _result;

    void Because() => _result = ValidationRulesExtractor
        .ExtractValidationRules(typeof(ValidatedModel).Assembly, typeof(ValidatedModel), includeConceptRules: false, includeDataAnnotations: false)
        .ToDictionary(_ => _.PropertyName);

    ValidationRuleDescriptor Single(string property) => _result[property].Rules.Single();

    [Fact] void should_describe_not_empty_with_its_custom_message() => Single("name").ErrorMessage.ShouldEqual("name required");
    [Fact] void should_name_the_not_empty_rule() => Single("name").RuleName.ShouldEqual("notEmpty");
    [Fact] void should_describe_email_address() => Single("email").RuleName.ShouldEqual("emailAddress");
    [Fact] void should_describe_length_with_both_bounds() => Single("bounded").Arguments.ShouldContainOnly(2, 5);
    [Fact] void should_describe_minimum_length() => Single("atLeast").Arguments.ShouldContainOnly(3);
    [Fact] void should_name_the_minimum_length_rule() => Single("atLeast").RuleName.ShouldEqual("minLength");
    [Fact] void should_describe_maximum_length() => Single("atMost").Arguments.ShouldContainOnly(8);
    [Fact] void should_name_the_maximum_length_rule() => Single("atMost").RuleName.ShouldEqual("maxLength");
    [Fact] void should_describe_exact_length_as_length_with_equal_bounds() => Single("exactly").Arguments.ShouldContainOnly(4, 4);
    [Fact] void should_describe_greater_than() => Single("number").RuleName.ShouldEqual("greaterThan");
    [Fact] void should_describe_less_than() => Single("small").RuleName.ShouldEqual("lessThan");
    [Fact] void should_describe_greater_than_or_equal() => Single("big").RuleName.ShouldEqual("greaterThanOrEqual");
    [Fact] void should_describe_less_than_or_equal() => Single("least").RuleName.ShouldEqual("lessThanOrEqual");
    [Fact] void should_describe_the_comparison_value() => Single("number").Arguments.ShouldContainOnly(5);
    [Fact] void should_describe_regular_expression_as_matches() => Single("pattern").RuleName.ShouldEqual("matches");
    [Fact] void should_skip_rules_with_a_condition() => _result.ContainsKey("conditional").ShouldBeFalse();
    [Fact] void should_skip_non_numeric_comparisons() => _result.ContainsKey("date").ShouldBeFalse();
    [Fact] void should_skip_properties_without_a_validator_rule() => _result.ContainsKey("unvalidated").ShouldBeFalse();
    [Fact] void should_default_to_error_severity() => Single("name").Severity.ShouldEqual(3);
    [Fact] void should_describe_warning_severity() => Single("warned").Severity.ShouldEqual(2);
    [Fact] void should_describe_info_severity() => Single("informed").Severity.ShouldEqual(1);
}
