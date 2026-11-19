// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB.for_ConceptSerializer.given;

namespace Cratis.Arc.MongoDB.for_ConceptSerializer;

public class when_deserializing_a_decimal_concept_stored_as_a_non_finite_double : a_concept_serializer
{
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void should_overflow(double stored) =>
        Catch.Exception(() => Deserialize<DecimalConcept>(stored)).ShouldBeOfExactType<OverflowException>();
}
