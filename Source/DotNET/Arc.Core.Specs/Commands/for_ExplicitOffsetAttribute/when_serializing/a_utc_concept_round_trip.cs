// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_ExplicitOffsetAttribute.when_serializing;

public class a_utc_concept_round_trip : given.date_inputs
{
    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Utc)]
    public void should_write_and_read_utc_with_tick_precision(DateTimeKind kind)
    {
        var original = new DateTime(2026, 10, 16, 12, 30, 0, kind).AddTicks(1234567);
        var input = new ConceptUtcInput(new(original));
        var json = JsonSerializer.Serialize(input, Options);
        var result = JsonSerializer.Deserialize<ConceptUtcInput>(json, Options)!;
        result.From.Value.Kind.ShouldEqual(DateTimeKind.Utc);
        result.From.Value.ShouldEqual(kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(original, DateTimeKind.Utc) : original.ToUniversalTime());
        json.ShouldContain("Z\"");
    }
}
