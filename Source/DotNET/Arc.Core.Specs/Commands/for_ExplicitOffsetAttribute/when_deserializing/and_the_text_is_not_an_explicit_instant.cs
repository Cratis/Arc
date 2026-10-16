// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_ExplicitOffsetAttribute.when_deserializing;

public class and_the_text_is_not_an_explicit_instant : given.date_inputs
{
    [Theory]
    [InlineData("2026-10-16")]
    [InlineData("2026-10-16T12:30:00.12345678Z")]
    [InlineData("2026-10-16t12:30:00z")]
    [InlineData("not-a-date")]
    [InlineData("2026-10-16T12:30:00+02")]
    [InlineData("2026-10-16T12:30:00")]
    [InlineData("2026-10-16T12Z")]
    [InlineData("2026-10-16 12:30:00Z")]
    void should_refuse_the_value(string value) => Catch.Exception(() => JsonSerializer.Deserialize<OffsetInput>($$"""{"from":"{{value}}"}""", Options)).ShouldBeOfExactType<ValueRequiresExplicitOffset>();
}
