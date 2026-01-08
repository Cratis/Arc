// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_extracting_rules_for_type_with_validator_having_constructor_dependencies : Specification
{
    IEnumerable<PropertyValidationDescriptor> _result;

    void Because() => _result = ValidationRulesExtractor.ExtractValidationRules(typeof(TestCommandWithDependency).Assembly, typeof(TestCommandWithDependency));

    [Fact] void should_return_validation_descriptors() => _result.ShouldNotBeEmpty();
    [Fact] void should_have_descriptor_for_name_property() => _result.Any(_ => _.PropertyName == "name").ShouldBeTrue();
    [Fact] void should_have_descriptor_for_age_property() => _result.Any(_ => _.PropertyName == "age").ShouldBeTrue();
    [Fact] void should_have_not_empty_rule_for_name() => _result.First(_ => _.PropertyName == "name").Rules.Any(r => r.RuleName == "notEmpty").ShouldBeTrue();
    [Fact] void should_have_greater_than_or_equal_rule_for_age() => _result.First(_ => _.PropertyName == "age").Rules.Any(r => r.RuleName == "greaterThanOrEqual").ShouldBeTrue();
}
