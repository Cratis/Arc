// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_ConverterExtensions.when_converting_a_scalar;

public class with_values_that_convert : Specification
{
    object? _int;
    object? _nullableInt;
    object? _decimal;
    object? _enum;
    object? _concept;
    object? _version;
    object? _sameType;
    object? _string;

    void Because()
    {
        _int = "42".ConvertTo(typeof(int));
        _nullableInt = "42".ConvertTo(typeof(int?));
        _decimal = "1.5".ConvertTo(typeof(decimal));
        _enum = "green".ConvertTo(typeof(Color));
        _concept = "7".ConvertTo(typeof(Count));
        _version = "1.2.3".ConvertTo(typeof(Version));
        _sameType = 5.ConvertTo(typeof(int));
        _string = 5.ConvertTo(typeof(string));
    }

    [Fact] void should_parse_an_int() => _int.ShouldEqual(42);
    [Fact] void should_parse_a_nullable_int() => _nullableInt.ShouldEqual(42);
    [Fact] void should_parse_a_decimal_with_the_invariant_culture() => _decimal.ShouldEqual(1.5m);
    [Fact] void should_parse_an_enum_ignoring_case() => _enum.ShouldEqual(Color.Green);
    [Fact] void should_create_the_concept() => _concept.ShouldEqual(new Count(7));
    [Fact] void should_convert_through_the_type_converter() => _version.ShouldEqual(new Version(1, 2, 3));
    [Fact] void should_return_a_value_of_the_target_type_as_is() => _sameType.ShouldEqual(5);
    [Fact] void should_convert_to_its_string_representation() => _string.ShouldEqual("5");
}
