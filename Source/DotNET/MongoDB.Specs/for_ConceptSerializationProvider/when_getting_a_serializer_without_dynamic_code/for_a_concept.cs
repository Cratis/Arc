// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_ConceptSerializationProvider.when_getting_a_serializer_without_dynamic_code;

public class for_a_concept : Specification
{
    object _serializer;

    void Because() => _serializer = ConceptSerializationProvider.GetSerializerWithoutDynamicCode(typeof(ProviderConcept));

    [Fact] void should_return_a_concept_serializer_for_the_concept() => _serializer.ShouldBeOfExactType<ConceptSerializer<ProviderConcept>>();
}
