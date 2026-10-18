// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_ExplicitOffsetAttribute.when_deserializing;

public class a_concept_with_a_non_string_token : given.date_inputs
{
    [Theory]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("{}")]
    public void should_refuse_the_token(string token) => Catch.Exception(() => JsonSerializer.Deserialize<ConceptOffsetInput>($$"""{"from":{{token}}}""", Options)).ShouldBeOfExactType<JsonException>();
}
