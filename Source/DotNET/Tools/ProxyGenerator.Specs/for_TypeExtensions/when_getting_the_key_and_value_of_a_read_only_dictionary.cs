// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

public class when_getting_the_key_and_value_of_a_read_only_dictionary : Specification
{
    Type _keyType = null!;
    Type _valueType = null!;

    void Because()
    {
        _keyType = typeof(IReadOnlyDictionary<string, int>).GetDictionaryKeyType();
        _valueType = typeof(IReadOnlyDictionary<string, int>).GetDictionaryValueType();
    }

    [Fact] void should_get_the_key_type() => _keyType.ShouldEqual(typeof(string));
    [Fact] void should_get_the_value_type() => _valueType.ShouldEqual(typeof(int));
}
