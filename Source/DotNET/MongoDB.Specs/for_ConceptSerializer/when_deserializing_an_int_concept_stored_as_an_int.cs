// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB.for_ConceptSerializer.given;

namespace Cratis.Arc.MongoDB.for_ConceptSerializer;

public class when_deserializing_an_int_concept_stored_as_an_int : a_concept_serializer
{
    IntConcept _result;

    void Because() => _result = Deserialize<IntConcept>(42);

    [Fact] void should_read_the_int_value() => _result.Value.ShouldEqual(42);
}
