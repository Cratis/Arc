// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Commands.for_ExplicitOffsetAttribute.when_serializing;

public class unspecified_local_and_utc_kinds : given.date_inputs
{
    static readonly DateTime _moment = new(2026, 10, 16, 12, 30, 0);

    DateTime[] _results;
    DateTime?[] _nullableResults;
    string[] _written;

    void Because()
    {
        var kinds = new[] { DateTimeKind.Unspecified, DateTimeKind.Local, DateTimeKind.Utc };
        var inputs = kinds.Select(kind => DateTime.SpecifyKind(_moment, kind)).ToArray();
        var json = inputs.Select(input => JsonSerializer.Serialize(new UtcInput(input), Options)).ToArray();
        var nullableJson = inputs.Select(input => JsonSerializer.Serialize(new NullableUtcInput(input), Options)).ToArray();
        _written = json;
        _results = json.Select(value => JsonSerializer.Deserialize<UtcInput>(value, Options)!.From).ToArray();
        _nullableResults = nullableJson.Select(value => JsonSerializer.Deserialize<NullableUtcInput>(value, Options)!.From).ToArray();
    }

    [Fact] void should_write_an_offset_for_every_kind() => _written.All(value => value.Contains("Z\"")).ShouldBeTrue();
    [Fact] void should_read_every_kind_as_utc() => _results.All(value => value.Kind == DateTimeKind.Utc).ShouldBeTrue();
    [Fact] void should_treat_unspecified_as_utc() => _results[0].ShouldEqual(DateTime.SpecifyKind(_moment, DateTimeKind.Utc));
    [Fact] void should_convert_local_to_utc() => _results[1].ShouldEqual(DateTime.SpecifyKind(_moment, DateTimeKind.Local).ToUniversalTime());
    [Fact] void should_preserve_utc() => _results[2].ShouldEqual(DateTime.SpecifyKind(_moment, DateTimeKind.Utc));
    [Fact] void should_round_trip_nullable_values() => _nullableResults.Select(value => value!.Value).ShouldEqual(_results);
}
