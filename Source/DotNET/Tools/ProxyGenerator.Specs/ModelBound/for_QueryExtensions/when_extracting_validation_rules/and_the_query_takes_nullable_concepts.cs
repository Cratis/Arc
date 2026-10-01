// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.when_extracting_validation_rules;

public class and_the_query_takes_nullable_concepts : Specification
{
    QueryDescriptor _result;

    void Because() => _result = typeof(ReadModelWithNullableConcept).GetTypeInfo().ToQueryDescriptors(
        "/output",
        segmentsToSkip: 5,
        skipQueryNameInRoute: true,
        apiPrefix: "api",
        [typeof(ReadModelWithNullableConcept).GetTypeInfo()]).Single();

    [Fact] void should_skip_inferred_rules_for_the_nullable_parameter() => _result.ValidationRules.Select(_ => _.PropertyName).ShouldNotContain("optional");
    [Fact] void should_keep_rules_for_the_required_parameter() => _result.ValidationRules.Single(_ => _.PropertyName == "required").Rules.Single().RuleName.ShouldEqual("notEmpty");
    [Fact] void should_keep_explicit_annotations_for_the_nullable_parameter() => _result.ValidationRules.Single(_ => _.PropertyName == "annotated").Rules.Single().RuleName.ShouldEqual("notEmpty");
    [Fact] void should_omit_conditional_concept_rules() => _result.ValidationRules.Single(_ => _.PropertyName == "conditional").Rules.Select(_ => _.RuleName).ShouldContainOnly("maxLength", "minLength");
}
