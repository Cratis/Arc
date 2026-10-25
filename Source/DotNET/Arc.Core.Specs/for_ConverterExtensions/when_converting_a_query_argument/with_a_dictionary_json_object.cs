// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_ConverterExtensions.when_converting_a_query_argument;

public class with_a_dictionary_json_object
{
    [Theory]
    [InlineData(typeof(Dictionary<string, string>))]
    [InlineData(typeof(IDictionary<string, string>))]
    [InlineData(typeof(IReadOnlyDictionary<string, string>))]
    public void should_preserve_json_literal_strings(Type targetType)
    {
        var result = (IDictionary<string, string>)"""{"quoted":"\"abc\"","number":"42","comma":"a,b"}"""
            .ConvertQueryArgument(targetType, "parts", "Some.Query")!;
        result["quoted"].ShouldEqual("\"abc\"");
        result["number"].ShouldEqual("42");
        result["comma"].ShouldEqual("a,b");
    }

    [Fact]
    public void should_convert_non_string_values()
    {
        var result = (Dictionary<string, int?>)"""{"count":42,"missing":null}"""
            .ConvertQueryArgument(typeof(Dictionary<string, int?>), "parts", "Some.Query")!;
        result["count"].ShouldEqual(42);
        result["missing"].ShouldBeNull();
    }

    [Fact]
    public void should_convert_nullable_enum_values()
    {
        var result = (IReadOnlyDictionary<string, DayOfWeek?>)"""{"day":"Monday","missing":null}"""
            .ConvertQueryArgument(typeof(IReadOnlyDictionary<string, DayOfWeek?>), "parts", "Some.Query")!;
        result["day"].ShouldEqual(DayOfWeek.Monday);
        result["missing"].ShouldBeNull();
    }

    [Fact]
    public void should_accept_an_empty_object() =>
        ((IDictionary<string, string>)"{}".ConvertQueryArgument(typeof(IDictionary<string, string>), "parts", "Some.Query")!).Count.ShouldEqual(0);
}
