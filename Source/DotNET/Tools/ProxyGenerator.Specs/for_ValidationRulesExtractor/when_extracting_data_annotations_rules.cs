// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_extracting_data_annotations_rules : Specification
{
    IDictionary<string, PropertyValidationDescriptor> _result;

    void Because() => _result = ValidationRulesExtractor.ExtractDataAnnotationsRules(typeof(AnnotatedModel)).ToDictionary(_ => _.PropertyName);

    ValidationRuleDescriptor Single(string property) => _result[property].Rules.Single();

    [Fact] void should_not_describe_properties_without_validation_attributes() => _result.ContainsKey("ignored").ShouldBeFalse();
    [Fact] void should_describe_required_as_not_empty() => Single("required").RuleName.ShouldEqual("notEmpty");
    [Fact] void should_carry_the_error_message() => Single("required").ErrorMessage.ShouldEqual("required message");
    [Fact] void should_describe_string_length_with_both_bounds_as_length() => Single("bothLengths").Arguments.ShouldContainOnly(2, 10);
    [Fact] void should_describe_string_length_with_only_a_minimum_as_min_length() => Single("minimumOnly").Arguments.ShouldContainOnly(3);
    [Fact] void should_name_the_minimum_only_string_length_min_length() => Single("minimumOnly").RuleName.ShouldEqual("minLength");
    [Fact] void should_describe_string_length_with_only_a_maximum_as_max_length() => Single("maximumOnly").RuleName.ShouldEqual("maxLength");
    [Fact] void should_describe_min_length() => Single("minLengthAttribute").RuleName.ShouldEqual("minLength");
    [Fact] void should_describe_max_length() => Single("maxLengthAttribute").RuleName.ShouldEqual("maxLength");
    [Fact] void should_describe_range_as_two_rules() => _result["ranged"].Rules.Select(_ => _.RuleName).ShouldContainOnly("greaterThanOrEqual", "lessThanOrEqual");
    [Fact] void should_describe_regular_expression_as_matches() => Single("pattern").RuleName.ShouldEqual("matches");
    [Fact] void should_describe_email_address() => Single("email").RuleName.ShouldEqual("emailAddress");
    [Fact] void should_describe_phone() => Single("phone").RuleName.ShouldEqual("phone");
    [Fact] void should_describe_url() => Single("url").RuleName.ShouldEqual("url");
    [Fact] void should_describe_credit_card() => Single("card").RuleName.ShouldEqual("creditCard");
}
