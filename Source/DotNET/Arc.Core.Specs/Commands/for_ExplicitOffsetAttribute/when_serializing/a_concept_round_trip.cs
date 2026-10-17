// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_ExplicitOffsetAttribute.when_serializing;

public class a_concept_round_trip : given.date_inputs
{
    ConceptOffsetInput _original;
    ConceptOffsetInput _result;

    void Establish() => _original = new(new(new DateTimeOffset(2026, 10, 16, 12, 30, 0, TimeSpan.FromHours(2)).AddTicks(1234567)));

    void Because() => _result = JsonSerializer.Deserialize<ConceptOffsetInput>(JsonSerializer.Serialize(_original, Options), Options)!;

    [Fact] void should_preserve_the_concept() => _result.ShouldEqual(_original);
    [Fact] void should_preserve_the_offset() => _result.From.Value.Offset.ShouldEqual(_original.From.Value.Offset);
    [Fact] void should_preserve_tick_precision() => _result.From.Value.Ticks.ShouldEqual(_original.From.Value.Ticks);
}
