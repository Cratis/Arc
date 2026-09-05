// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_getting_target_type_for_generic_type_parameters;

public class and_type_is_generic_type_parameter : Specification
{
    TargetType _result = null!;

    void Because()
    {
        // Get the generic type parameter T from a generic type definition
        var genericType = typeof(List<>);
        var typeParameter = genericType.GetGenericArguments()[0];
        _result = typeParameter.GetTargetType();
    }

    [Fact] void should_not_throw_null_reference_exception() => _result.ShouldNotBeNull();
    [Fact] void should_have_type_name() => _result.Type.ShouldEqual("T");
    [Fact] void should_have_constructor_name() => _result.Constructor.ShouldEqual("T");
}
