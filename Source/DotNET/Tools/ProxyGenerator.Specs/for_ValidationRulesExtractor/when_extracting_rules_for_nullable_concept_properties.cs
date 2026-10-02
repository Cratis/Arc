// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_extracting_rules_for_nullable_concept_properties : Specification
{
    IEnumerable<PropertyValidationDescriptor> _result;

    void Because() => _result = ValidationRulesExtractor.ExtractValidationRules(
        typeof(TestCommandWithNullableConcepts).Assembly,
        typeof(TestCommandWithNullableConcepts));

    [Fact] void should_skip_inferred_rules_for_the_nullable_property() => _result.Select(_ => _.PropertyName).ShouldNotContain("optional");
    [Fact] void should_keep_only_null_tolerant_inferred_rules_for_the_nullable_property() => _result.Single(_ => _.PropertyName == "limited").Rules.Select(_ => _.RuleName).ShouldContainOnly("maxLength");
    [Fact] void should_keep_the_inferred_length_limit() => _result.Single(_ => _.PropertyName == "limited").Rules.Single().Arguments.Single().ShouldEqual(10);
    [Fact] void should_project_rules_for_the_required_property() => _result.Single(_ => _.PropertyName == "required").Rules.Single().RuleName.ShouldEqual("notEmpty");
    [Fact] void should_keep_only_the_explicit_command_rule_for_the_nullable_property() => _result.Single(_ => _.PropertyName == "explicit").Rules.Select(_ => _.RuleName).ShouldContainOnly("notNull");
    [Fact] void should_keep_explicit_annotations_on_the_nullable_property() => _result.Single(_ => _.PropertyName == "annotated").Rules.Single().RuleName.ShouldEqual("notEmpty");
}
