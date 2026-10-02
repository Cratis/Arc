// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ControllerBased.for_QueryExtensions.when_extracting_validation_rules;

public class and_nullable_concepts_have_a_matching_dto : Specification
{
    QueryDescriptor _result;

    void Because() => _result = typeof(NullableConceptTestController).GetMethod(nameof(NullableConceptTestController.Find))!
        .ToQueryDescriptor("/output", segmentsToSkip: 5);

    [Fact] void should_omit_presence_rules_despite_the_non_nullable_dto_property() => _result.ValidationRules.Select(_ => _.PropertyName).ShouldNotContain("controllerOptional");
    [Fact] void should_emit_the_nullable_parameter_as_optional() => _result.Parameters.Single(_ => _.Name == "controllerOptional").IsOptional.ShouldBeTrue();
    [Fact] void should_keep_presence_rules_despite_the_nullable_dto_property() => _result.ValidationRules.Single(_ => _.PropertyName == "controllerRequired").Rules.Select(_ => _.RuleName).ShouldContainOnly("notEmpty");
    [Fact] void should_keep_only_null_tolerant_rules_despite_the_non_nullable_dto_property() => _result.ValidationRules.Single(_ => _.PropertyName == "controllerLimited").Rules.Select(_ => _.RuleName).ShouldContainOnly("maxLength");
    [Fact] void should_keep_explicit_rules_alongside_inferred_concept_rules() => _result.ValidationRules.Single(_ => _.PropertyName == "controllerExplicit").Rules.Select(_ => _.RuleName).ShouldContainOnly("notNull", "notEmpty", "maxLength");
    [Fact] void should_not_promote_dto_annotations_above_non_nullable_concept_rules() => _result.ValidationRules.Single(_ => _.PropertyName == "controllerMaxOnly").Rules.Select(_ => _.RuleName).ShouldContainOnly("maxLength");
    [Fact] void should_not_promote_dto_annotations_above_nullable_concept_rules() => _result.ValidationRules.Single(_ => _.PropertyName == "controllerNullableMaxOnly").Rules.Select(_ => _.RuleName).ShouldContainOnly("maxLength");
    [Fact] void should_keep_presence_and_length_rules_on_the_explicitly_required_nullable_parameter() => _result.ValidationRules.Single(_ => _.PropertyName == "controllerAnnotatedLimited").Rules.Select(_ => _.RuleName).ShouldContainOnly("notEmpty", "maxLength");
    [Fact] void should_keep_dto_annotations_when_no_concept_rules_remain() => _result.ValidationRules.Single(_ => _.PropertyName == "controllerDefaulted").Rules.Select(_ => _.RuleName).ShouldContainOnly("notEmpty");
}
