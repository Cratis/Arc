// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_extracting_rules_for_constructor_bound_concept_properties : Specification
{
    IEnumerable<PropertyValidationDescriptor> _result;

    void Because() => _result = ValidationRulesExtractor.ExtractValidationRules(
        typeof(TestCommandWithConstructorBoundConcepts).Assembly,
        typeof(TestCommandWithConstructorBoundConcepts));

    [Fact] void should_omit_inferred_presence_rules_for_the_nullable_getter_only_property() => _result.Select(_ => _.PropertyName).ShouldNotContain("optional");
    [Fact] void should_keep_inferred_presence_rules_for_the_required_getter_only_property() => _result.Single(_ => _.PropertyName == "required").Rules.Select(_ => _.RuleName).ShouldContainOnly("notEmpty");
    [Fact] void should_keep_null_tolerant_rules_for_the_nullable_getter_only_property() => _result.Single(_ => _.PropertyName == "limited").Rules.Select(_ => _.RuleName).ShouldContainOnly("maxLength");
}
