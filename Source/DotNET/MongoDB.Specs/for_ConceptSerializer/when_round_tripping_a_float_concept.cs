// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB.for_ConceptSerializer.given;

namespace Cratis.Arc.MongoDB.for_ConceptSerializer;

public class when_round_tripping_a_float_concept : a_concept_serializer
{
    FloatConcept _result;
    Exception _error;

    void Because() => _error = Catch.Exception(() => _result = Roundtrip(new FloatConcept(3.5f)));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_preserve_the_value() => _result.Value.ShouldEqual(3.5f);
}
