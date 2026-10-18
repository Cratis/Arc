// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_ExplicitOffsetAttribute.when_deserializing;

public class a_utc_concept : given.date_inputs
{
    ConceptUtcInput _result;

    void Because() => _result = JsonSerializer.Deserialize<ConceptUtcInput>("""{"from":"2026-10-16T12:30:00.1234567+02:00"}""", Options)!;

    [Fact] void should_normalize_to_utc() => _result.From.Value.ShouldEqual(new DateTime(2026, 10, 16, 10, 30, 0, DateTimeKind.Utc).AddTicks(1234567));
    [Fact] void should_have_utc_kind() => _result.From.Value.Kind.ShouldEqual(DateTimeKind.Utc);
}
