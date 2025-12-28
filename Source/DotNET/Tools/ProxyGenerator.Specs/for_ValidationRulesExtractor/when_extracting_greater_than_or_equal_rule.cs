// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_extracting_greater_than_or_equal_rule : Specification
{
    PropertyValidationDescriptor _ageDescriptor;
    ValidationRuleDescriptor _rule;

    void Establish()
    {
        var result = ValidationRulesExtractor.ExtractValidationRules(typeof(TestCommand).Assembly, typeof(TestCommand));
        _ageDescriptor = result.First(_ => _.PropertyName == "age");
    }

    void Because() => _rule = _ageDescriptor.Rules.First();

    [Fact] void should_have_greater_than_or_equal_rule_name() => _rule.RuleName.ShouldEqual("greaterThanOrEqual");

    [Fact] void should_have_threshold_argument() => _rule.Arguments.ShouldContain(18);
}
