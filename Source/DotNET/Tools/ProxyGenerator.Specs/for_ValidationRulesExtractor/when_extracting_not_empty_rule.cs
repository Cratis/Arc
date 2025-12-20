// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_extracting_not_empty_rule : Specification
{
    PropertyValidationDescriptor _nameDescriptor;
    ValidationRuleDescriptor _rule;

    void Establish()
    {
        var result = ValidationRulesExtractor.ExtractValidationRules(typeof(TestCommand).Assembly, typeof(TestCommand));
        _nameDescriptor = result.First(_ => _.PropertyName == "name");
    }

    void Because() => _rule = _nameDescriptor.Rules.First();

    [Fact] void should_have_not_empty_rule_name() => _rule.RuleName.ShouldEqual("notEmpty");

    [Fact] void should_have_no_arguments() => _rule.Arguments.ShouldBeEmpty();
}
