// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_extracting_validation_rules;

public class and_nullable_concepts_also_have_an_argument_set : Specification
{
    QueryDescriptor _result;

    void Because() => _result = typeof(ReadModelWithNullableConceptAndParameters).GetTypeInfo().ToQueryDescriptors(
        "/output",
        segmentsToSkip: 5,
        skipQueryNameInRoute: true,
        apiPrefix: "api",
        [typeof(ReadModelWithNullableConceptAndParameters).GetTypeInfo()]).Single();

    [Fact] void should_skip_inferred_rules_from_both_the_parameter_and_argument_model() => _result.ValidationRules.Select(_ => _.PropertyName).ShouldNotContain("optional");
    [Fact] void should_keep_the_explicit_query_rule_and_null_tolerant_inferred_rule() => _result.ValidationRules.Single(_ => _.PropertyName == "explicitName").Rules.Select(_ => _.RuleName).ShouldContainOnly("notNull", "maxLength");
    [Fact] void should_keep_only_null_tolerant_inferred_rules_despite_the_non_nullable_argument_model_property() => _result.ValidationRules.Single(_ => _.PropertyName == "limited").Rules.Select(_ => _.RuleName).ShouldContainOnly("maxLength");
    [Fact] void should_keep_presence_rules_for_the_required_parameter_despite_the_nullable_argument_model_property() => _result.ValidationRules.Single(_ => _.PropertyName == "required").Rules.Select(_ => _.RuleName).ShouldContainOnly("notEmpty");
    [Fact] void should_keep_argument_model_annotations() => _result.ValidationRules.Single(_ => _.PropertyName == "annotated").Rules.Select(_ => _.RuleName).ShouldContainOnly("notEmpty");
}
