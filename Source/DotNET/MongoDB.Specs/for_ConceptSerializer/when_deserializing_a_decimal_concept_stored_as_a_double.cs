// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.MongoDB.for_ConceptSerializer.given;

namespace Cratis.Arc.MongoDB.for_ConceptSerializer;

public class when_deserializing_a_decimal_concept_stored_as_a_double : a_concept_serializer
{
    [Theory]
    [InlineData(1.23d, "1.23")]
    [InlineData(0.1d, "0.1")]
    [InlineData(9007199254740991d, "9007199254740990")] // 16 digits round to 15 significant digits, as before .NET 11
    [InlineData(1e-7d, "0.0000001")]
    [InlineData(1e20d, "100000000000000000000")]
    [InlineData(7.9e28d, "79000000000000000000000000000")]
    public void should_coerce_to_the_decimal_text_value(double stored, string expected) =>
        Deserialize<DecimalConcept>(stored).Value.ShouldEqual(decimal.Parse(expected, CultureInfo.InvariantCulture));
}
