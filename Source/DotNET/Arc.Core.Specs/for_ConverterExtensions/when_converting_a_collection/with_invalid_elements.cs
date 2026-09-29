// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.for_ConverterExtensions.when_converting_a_collection;

public class with_invalid_elements : Specification
{
    Exception? _invalidInt;
    Exception? _invalidTimeSpan;
    Exception? _nullForValueType;
    Exception? _nested;

    void Because()
    {
        _invalidInt = Catch.Exception(() => "1,x".ConvertTo(typeof(int[])));
        _invalidTimeSpan = Catch.Exception(() => "00:01:00,bad".ConvertTo(typeof(TimeSpan[])));
        _nullForValueType = Catch.Exception(() => new object?[] { 1, null }.ConvertTo(typeof(int[])));
        _nested = Catch.Exception(() => "1".ConvertTo(typeof(int[][])));
    }

    [Fact] void should_reject_an_invalid_int_element() => _invalidInt.ShouldBeOfExactType<InvalidCollectionQueryArgument>();
    [Fact] void should_reject_an_element_the_type_converter_cannot_convert() => _invalidTimeSpan.ShouldBeOfExactType<InvalidCollectionQueryArgument>();
    [Fact] void should_reject_a_null_element_of_a_value_type() => _nullForValueType.ShouldBeOfExactType<InvalidCollectionQueryArgument>();
    [Fact] void should_reject_a_nested_collection() => _nested.ShouldBeOfExactType<InvalidCollectionQueryArgument>();
}
