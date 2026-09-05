// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB.for_ConceptSerializer.given;

namespace Cratis.Arc.MongoDB.for_ConceptSerializer;

public class when_deserializing_a_long_concept_stored_as_a_double : a_concept_serializer
{
    LongConcept _result;

    void Because() => _result = Deserialize<LongConcept>(42d);

    [Fact] void should_coerce_to_the_long_value() => _result.Value.ShouldEqual(42L);
}
