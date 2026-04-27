// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_PropertyExtensions.when_converting_property_to_descriptor;

public class with_dictionary_with_complex_key : Specification
{
    PropertyInfo _property;
    PropertyDescriptor _result;

    void Establish() => _property = typeof(TypeWithComplexKeyDictionary).GetProperty(nameof(TypeWithComplexKeyDictionary.Items));

    void Because() => _result = _property.ToPropertyDescriptor();

    [Fact] void should_have_correct_name() => _result.Name.ShouldEqual("Items");
    [Fact] void should_have_map_type() => _result.Type.ShouldEqual("Map<DictionaryKeyType, DictionaryValueType>");
    [Fact] void should_have_map_constructor() => _result.Constructor.ShouldEqual("Map");
    [Fact] void should_not_be_enumerable() => _result.IsEnumerable.ShouldBeFalse();
}
