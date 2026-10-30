// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_ConceptSerializationProvider.when_getting_a_serializer;

public class for_a_type_that_is_not_a_concept : Specification
{
    ConceptSerializationProvider _provider;
    object _serializer;

    void Establish() => _provider = new();

    void Because() => _serializer = _provider.GetSerializer(typeof(string));

    [Fact] void should_return_no_serializer() => _serializer.ShouldBeNull();
}
