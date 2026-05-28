// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.for_JsonSerializerOptionsConfiguration.when_doing_a_roundtrip_serialization;

public class with_value_being_negative_infinity : Specification
{
    JsonSerializerOptions _options;
    string _json;
    double _deserialized;

    void Establish() => _options = new JsonSerializerOptions().ConfigureArcDefaults();

    void Because()
    {
        _json = JsonSerializer.Serialize(double.NegativeInfinity, _options);
        _deserialized = JsonSerializer.Deserialize<double>(_json, _options);
    }

    [Fact] void should_round_trip_negative_infinity() => double.IsNegativeInfinity(_deserialized).ShouldBeTrue();
}