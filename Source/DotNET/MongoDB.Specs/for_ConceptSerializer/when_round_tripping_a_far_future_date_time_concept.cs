// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB.for_ConceptSerializer.given;

namespace Cratis.Arc.MongoDB.for_ConceptSerializer;

public class when_round_tripping_a_far_future_date_time_concept : a_concept_serializer
{
    static readonly DateTime _value = new(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);
    DateTimeConcept _result;
    Exception _error;

    void Because()
    {
        // A far-future value pins the epoch fix: the old write stored ticks-since-0001, which for a date this far
        // out exceeds what the deserializer's FromUnixTimeMilliseconds accepts, so reading it back threw.
        _error = Catch.Exception(() => _result = Roundtrip(new DateTimeConcept(_value)));
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_preserve_the_value() => _result.Value.ShouldEqual(_value);
}
