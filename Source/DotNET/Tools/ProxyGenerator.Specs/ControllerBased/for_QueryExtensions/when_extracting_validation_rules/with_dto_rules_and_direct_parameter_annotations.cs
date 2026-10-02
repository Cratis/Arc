// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ControllerBased.for_QueryExtensions.when_extracting_validation_rules;

public class with_dto_rules_and_direct_parameter_annotations : Specification
{
    QueryDescriptor _result;

    void Because() => _result = typeof(AnnotationFallbackTestController).GetMethod(nameof(AnnotationFallbackTestController.Find))!
        .ToQueryDescriptor("/output", 5);

    [Fact] void should_keep_the_dto_validation_rule() => _result.ValidationRules.Single().PropertyName.ShouldEqual("dtoValidatedName");
    [Fact] void should_keep_the_dto_presence_check() => _result.ValidationRules.Single().Rules.Single().RuleName.ShouldEqual("notEmpty");
    [Fact] void should_not_add_direct_parameter_annotations() => _result.ValidationRules.SelectMany(_ => _.Rules).Select(_ => _.RuleName).ShouldNotContain("creditCard");
}
