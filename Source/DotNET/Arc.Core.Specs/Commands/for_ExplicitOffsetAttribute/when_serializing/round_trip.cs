// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_ExplicitOffsetAttribute.when_serializing;

public class round_trip : given.date_inputs
{
    OffsetInput _original;
    OffsetInput _result;

    void Establish() => _original = new OffsetInput(new DateTimeOffset(2026, 10, 16, 12, 30, 0, TimeSpan.FromHours(2)).AddTicks(1234567));

    void Because() => _result = JsonSerializer.Deserialize<OffsetInput>(JsonSerializer.Serialize(_original, Options), Options)!;

    [Fact] void should_preserve_the_instant() => _result.ShouldEqual(_original);
    [Fact] void should_preserve_the_offset() => _result.From.Offset.ShouldEqual(_original.From.Offset);
}
