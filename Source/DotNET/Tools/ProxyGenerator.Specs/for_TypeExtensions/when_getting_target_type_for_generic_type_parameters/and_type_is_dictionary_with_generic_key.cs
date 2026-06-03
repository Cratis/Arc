// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_getting_target_type_for_generic_type_parameters;

public class and_type_is_dictionary_with_generic_key : Specification
{
    TargetType _result = null!;

    void Because()
    {
        // Dictionary with a generic type parameter as key
        var genericDictionaryType = typeof(Dictionary<,>);
        _result = genericDictionaryType.GetTargetType();
    }

    [Fact] void should_not_throw_null_reference_exception() => _result.ShouldNotBeNull();
    [Fact] void should_be_final() => _result.Final.ShouldBeTrue();
}
