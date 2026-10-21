// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_ExplicitOffsetAttribute.when_deserializing;

public class and_the_value_is_not_a_string : given.date_inputs
{
    [Theory]
    [InlineData("123")]
    [InlineData("null")]
    [InlineData("true")]
    void should_refuse_with_a_generic_json_failure(string token) => Catch.Exception(() => JsonSerializer.Deserialize<OffsetInput>($$"""{"from":{{token}}}""", Options)).ShouldBeOfExactType<JsonException>();

    [Fact] void should_accept_nullable_offset_null() => JsonSerializer.Deserialize<NullableOffsetInput>("""{"from":null}""", Options)!.From.ShouldBeNull();
    [Fact] void should_accept_nullable_date_time_null() => JsonSerializer.Deserialize<NullableUtcInput>("""{"from":null}""", Options)!.From.ShouldBeNull();
}
