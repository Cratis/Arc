// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_PropertyExtensions.when_converting_property_to_descriptor;

public class with_dictionary_with_collection_value : Specification
{
    PropertyInfo _property = null!;
    PropertyDescriptor _result = null!;

    void Establish() => _property = typeof(TypeWithCollectionValueDictionary).GetProperty(nameof(TypeWithCollectionValueDictionary.Slots))!;

    void Because() => _result = _property.ToPropertyDescriptor();

    [Fact] void should_have_correct_name() => _result.Name.ShouldEqual("Slots");
    [Fact] void should_have_record_type_with_array_value() => _result.Type.ShouldEqual("Record<string, DictionaryValueType[]>");
    [Fact] void should_have_object_constructor() => _result.Constructor.ShouldEqual("Object");
    [Fact] void should_not_be_enumerable() => _result.IsEnumerable.ShouldBeFalse();
}
