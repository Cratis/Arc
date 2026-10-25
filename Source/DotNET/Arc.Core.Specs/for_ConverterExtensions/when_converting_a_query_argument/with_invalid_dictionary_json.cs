// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.for_ConverterExtensions.when_converting_a_query_argument;

public class with_invalid_dictionary_json
{
    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("{\"count\":\"bad\"}")]
    [InlineData("{\"count\":{}}")]
    [InlineData("{\"count\":null}")]
    public void should_reject_invalid_input(string json) =>
        Catch.Exception(() => json.ConvertQueryArgument(typeof(IDictionary<string, int>), "parts", "Some.Query"))
            .ShouldBeOfExactType<InvalidQueryArgument>();
}
