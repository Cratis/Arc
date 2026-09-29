// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_ConverterExtensions.when_converting_a_scalar;

public class with_values_that_do_not_convert : Specification
{
    object? _nullToInt;
    object? _nullToString;
    object? _invalidInt;
    object? _emptyInt;
    object? _invalidNullableInt;
    object? _invalidGuid;
    object? _invalidDateTime;
    object? _invalidEnum;
    object? _invalidConcept;
    object? _invalidVersion;

    void Because()
    {
        _nullToInt = ((object)null!).ConvertTo(typeof(int));
        _nullToString = ((object)null!).ConvertTo(typeof(string));
        _invalidInt = "x".ConvertTo(typeof(int));
        _emptyInt = string.Empty.ConvertTo(typeof(int));
        _invalidNullableInt = "x".ConvertTo(typeof(int?));
        _invalidGuid = "x".ConvertTo(typeof(Guid));
        _invalidDateTime = "x".ConvertTo(typeof(DateTime));
        _invalidEnum = "blue".ConvertTo(typeof(Color));
        _invalidConcept = "x".ConvertTo(typeof(Count));
        _invalidVersion = "x".ConvertTo(typeof(Version));
    }

    [Fact] void should_default_null_to_the_value_type_default() => _nullToInt.ShouldEqual(0);
    [Fact] void should_leave_null_for_a_reference_type() => _nullToString.ShouldBeNull();
    [Fact] void should_default_an_invalid_int() => _invalidInt.ShouldEqual(0);
    [Fact] void should_default_an_empty_int() => _emptyInt.ShouldEqual(0);
    [Fact] void should_leave_an_invalid_nullable_int_null() => _invalidNullableInt.ShouldBeNull();
    [Fact] void should_default_an_invalid_guid() => _invalidGuid.ShouldEqual(Guid.Empty);
    [Fact] void should_default_an_invalid_date_time() => _invalidDateTime.ShouldEqual(default(DateTime));
    [Fact] void should_default_an_invalid_enum() => _invalidEnum.ShouldEqual(Color.Red);
    [Fact] void should_not_create_a_concept_from_an_invalid_value() => _invalidConcept.ShouldBeNull();
    [Fact] void should_leave_an_invalid_type_converter_value_null() => _invalidVersion.ShouldBeNull();
}
