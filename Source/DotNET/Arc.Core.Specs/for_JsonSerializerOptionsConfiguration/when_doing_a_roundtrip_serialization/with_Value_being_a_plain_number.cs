// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.for_JsonSerializerOptionsConfiguration.when_doing_a_roundtrip_serialization;

public class with_Value_being_a_plain_number : Specification
{
    JsonSerializerOptions _options;
    string _json;
    double _deserialized;

    void Establish() => _options = new JsonSerializerOptions().ConfigureArcDefaults();

    void Because()
    {
        _json = JsonSerializer.Serialize(42.5, _options);
        _deserialized = JsonSerializer.Deserialize<double>(_json, _options);
    }

    [Fact] void should_serialize_as_a_plain_number() => _json.ShouldEqual("42.5");
    [Fact] void should_round_trip_the_value() => _deserialized.ShouldEqual(42.5);
}