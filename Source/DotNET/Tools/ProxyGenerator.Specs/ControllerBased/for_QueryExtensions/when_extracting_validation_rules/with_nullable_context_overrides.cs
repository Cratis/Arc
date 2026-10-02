// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ControllerBased.for_QueryExtensions.when_extracting_validation_rules;

public class with_nullable_context_overrides : Specification
{
    MethodInfo _nullableMethod;
    MethodInfo _nonNullableMethod;
    QueryDescriptor _nullableContext;
    QueryDescriptor _nonNullableContext;

    void Establish()
    {
        _nullableMethod = typeof(NullabilityContextTestController).GetMethod(nameof(NullabilityContextTestController.NullableContext))!;
        _nonNullableMethod = typeof(NullabilityContextTestController).GetMethod(nameof(NullabilityContextTestController.NonNullableContext))!;
    }

    void Because()
    {
        _nullableContext = _nullableMethod.ToQueryDescriptor("/output", 5);
        _nonNullableContext = _nonNullableMethod.ToQueryDescriptor("/output", 5);
    }

    [Fact] void should_exercise_a_nullable_method_context() => _nullableMethod.GetCustomAttributesData().Single(_ => _.AttributeType.Name == "NullableContextAttribute").ConstructorArguments.Single().Value.ShouldEqual((byte)2);
    [Fact] void should_exercise_a_non_nullable_parameter_override() => _nullableMethod.GetParameters()[1].GetCustomAttributesData().Single(_ => _.AttributeType.Name == "NullableAttribute").ConstructorArguments.Single().Value.ShouldEqual((byte)1);
    [Fact] void should_exercise_a_non_nullable_method_context() => _nonNullableMethod.GetCustomAttributesData().Single(_ => _.AttributeType.Name == "NullableContextAttribute").ConstructorArguments.Single().Value.ShouldEqual((byte)1);
    [Fact] void should_exercise_a_nullable_parameter_override() => _nonNullableMethod.GetParameters()[1].GetCustomAttributesData().Single(_ => _.AttributeType.Name == "NullableAttribute").ConstructorArguments.Single().Value.ShouldEqual((byte)2);
    [Fact] void should_mark_the_contextually_nullable_parameter_as_optional() => _nullableContext.Parameters.Single(_ => _.Name == "contextualOptional").IsOptional.ShouldBeTrue();
    [Fact] void should_omit_presence_rules_for_the_contextually_nullable_parameter() => _nullableContext.ValidationRules.Select(_ => _.PropertyName).ShouldNotContain("contextualOptional");
    [Fact] void should_mark_the_non_nullable_override_as_required() => _nullableContext.Parameters.Single(_ => _.Name == "nonNullableOverride").IsOptional.ShouldBeFalse();
    [Fact] void should_keep_presence_rules_for_the_non_nullable_override() => _nullableContext.ValidationRules.Single(_ => _.PropertyName == "nonNullableOverride").Rules.Select(_ => _.RuleName).ShouldContainOnly("notEmpty");
    [Fact] void should_mark_the_contextually_non_nullable_parameter_as_required() => _nonNullableContext.Parameters.Single(_ => _.Name == "contextualRequired").IsOptional.ShouldBeFalse();
    [Fact] void should_keep_presence_rules_for_the_contextually_non_nullable_parameter() => _nonNullableContext.ValidationRules.Single(_ => _.PropertyName == "contextualRequired").Rules.Select(_ => _.RuleName).ShouldContainOnly("notEmpty");
    [Fact] void should_mark_the_nullable_override_as_optional() => _nonNullableContext.Parameters.Single(_ => _.Name == "nullableOverride").IsOptional.ShouldBeTrue();
    [Fact] void should_omit_presence_rules_for_the_nullable_override() => _nonNullableContext.ValidationRules.Select(_ => _.PropertyName).ShouldNotContain("nullableOverride");
}
