// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_extracting_email_rule : Specification
{
    PropertyValidationDescriptor _emailDescriptor;
    ValidationRuleDescriptor _rule;

    void Establish()
    {
        var result = ValidationRulesExtractor.ExtractValidationRules(typeof(TestCommand).Assembly, typeof(TestCommand));
        _emailDescriptor = result.First(_ => _.PropertyName == "email");
    }

    void Because() => _rule = _emailDescriptor.Rules.First();

    [Fact] void should_have_email_address_rule_name() => _rule.RuleName.ShouldEqual("emailAddress");

    [Fact] void should_have_no_arguments() => _rule.Arguments.ShouldBeEmpty();
}
