// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_ExplicitOffsetAttribute.when_deserializing;

public class and_the_value_is_an_instant : given.date_inputs
{
    [Theory]
    [InlineData("2026-10-16T12:30:00.1234567+02:00", 2, 1234567)]
    [InlineData("2026-10-16T12:30:00Z", 0, 0)]
    [InlineData("2026-10-16T12:30+02:00", 2, 0)]
    [InlineData("2026-10-16T12:30Z", 0, 0)]
    [InlineData("2026-10-16T12:30:00+0200", 2, 0)]
    [InlineData("2026-10-16T12:30:00+2:00", 2, 0)]
    [InlineData("2026-10-16T12:30:00-02:00", -2, 0)]
    void should_preserve_offset_and_tick_precision(string value, int offsetHours, long fractionTicks)
    {
        var result = JsonSerializer.Deserialize<OffsetInput>($$"""{"from":"{{value}}"}""", Options)!;
        result.From.Offset.ShouldEqual(TimeSpan.FromHours(offsetHours));
        (result.From.Ticks % TimeSpan.TicksPerSecond).ShouldEqual(fractionTicks);
    }

    [Fact] void should_convert_date_time_to_utc() => JsonSerializer.Deserialize<UtcInput>("""{"from":"2026-10-16T12:30:00.1234567+02:00"}""", Options)!.From.ShouldEqual(new DateTime(2026, 10, 16, 10, 30, 0, DateTimeKind.Utc).AddTicks(1234567));
    [Fact] void should_set_utc_kind() => JsonSerializer.Deserialize<UtcInput>("""{"from":"2026-10-16T12:30Z"}""", Options)!.From.Kind.ShouldEqual(DateTimeKind.Utc);
    [Fact] void should_read_nullable_offset_values() => JsonSerializer.Deserialize<NullableOffsetInput>("""{"from":"2026-10-16T12:30+02:00"}""", Options)!.From!.Value.Offset.ShouldEqual(TimeSpan.FromHours(2));
    [Fact] void should_read_nullable_date_time_values() => JsonSerializer.Deserialize<NullableUtcInput>("""{"from":"2026-10-16T12:30+02:00"}""", Options)!.From!.Value.Kind.ShouldEqual(DateTimeKind.Utc);
}
