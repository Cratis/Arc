// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ControllerBased.for_QueryExtensions.when_extracting_validation_rules;

public class without_dto_rules : Specification
{
    QueryDescriptor _result;

    void Because() => _result = typeof(AnnotationFallbackTestController).GetMethod(nameof(AnnotationFallbackTestController.WithoutDtoRules))!
        .ToQueryDescriptor("/output", 5);

    [Fact] void should_fall_back_to_direct_parameter_annotations() => _result.ValidationRules.Single().PropertyName.ShouldEqual("fallbackName");
    [Fact] void should_keep_the_annotation_presence_check() => _result.ValidationRules.Single().Rules.Single().RuleName.ShouldEqual("notEmpty");
}
