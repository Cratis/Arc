// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB.for_ConceptSerializer.given;

namespace Cratis.Arc.MongoDB.for_ConceptSerializer;

public class when_round_tripping_a_date_time_concept : a_concept_serializer
{
    static readonly DateTime _value = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
    DateTimeConcept _result;

    void Because()
    {
        // A UTC value keeps the round-trip independent of the machine's local time zone: the serializer normalizes
        // to UTC on the way out and the deserializer hands back the same wall clock.
        _result = Roundtrip(new DateTimeConcept(_value));
    }

    [Fact] void should_preserve_the_value() => _result.Value.ShouldEqual(_value);
}
