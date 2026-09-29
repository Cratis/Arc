// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.for_ConverterExtensions.when_converting_a_query_argument;

public class with_scalar_values : Specification
{
    const string QueryName = "Some.Query";

    object? _valid;
    object? _missing;
    object? _emptyStringConcept;
    object? _emptyForInt;
    Exception? _invalid;
    Exception? _invalidTypeConverterValue;
    Exception? _invalidCollection;

    void Because()
    {
        _valid = "42".ConvertQueryArgument(typeof(int), "count", QueryName);
        _missing = ((object?)null).ConvertQueryArgument(typeof(int), "count", QueryName);
        _emptyStringConcept = string.Empty.ConvertQueryArgument(typeof(Identity.IdentityName), "name", QueryName);
        _emptyForInt = string.Empty.ConvertQueryArgument(typeof(int), "count", QueryName);
        _invalid = Catch.Exception(() => "x".ConvertQueryArgument(typeof(int), "count", QueryName));
        _invalidTypeConverterValue = Catch.Exception(() => "x".ConvertQueryArgument(typeof(Version), "version", QueryName));
        _invalidCollection = Catch.Exception(() => "1,x".ConvertQueryArgument(typeof(int[]), "counts", QueryName));
    }

    [Fact] void should_convert_a_valid_value() => _valid.ShouldEqual(42);
    [Fact] void should_leave_a_missing_value_null() => _missing.ShouldBeNull();
    [Fact] void should_create_an_empty_string_concept() => _emptyStringConcept.ShouldEqual(Identity.IdentityName.Empty);
    [Fact] void should_leave_an_empty_value_as_is_for_the_performer() => _emptyForInt.ShouldEqual(string.Empty);
    [Fact] void should_reject_an_invalid_value() => _invalid.ShouldBeOfExactType<InvalidQueryArgument>();
    [Fact] void should_reject_a_value_the_type_converter_cannot_convert() => _invalidTypeConverterValue.ShouldBeOfExactType<InvalidQueryArgument>();
    [Fact] void should_reject_an_invalid_collection() => _invalidCollection.ShouldBeOfExactType<InvalidQueryArgument>();
}
