// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_is_enumerable_of_primitive_or_concept;

public class with_runtime_parity : Specification
{
    public record SampleConcept(string Value) : Cratis.Concepts.ConceptAs<string>(Value);

    [Fact]
    void should_match_runtime_for_all_supported_scalar_and_other_shapes()
    {
        Type[] elementTypes =
        [
            typeof(object), typeof(char), typeof(byte), typeof(sbyte), typeof(bool), typeof(string),
            typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset),
            typeof(Guid), typeof(TimeSpan), typeof(DateOnly), typeof(TimeOnly), typeof(Uri),
            typeof(JsonNode), typeof(JsonObject), typeof(JsonArray), typeof(JsonDocument),
            typeof(int?), typeof(DateOnly?), typeof(SampleConcept), typeof(DayOfWeek),
            typeof(int[]), typeof(Dictionary<string, int>)
        ];

        foreach (var elementType in elementTypes)
        {
            var collection = typeof(IEnumerable<>).MakeGenericType(elementType);
            Assert.True(
                collection.IsEnumerableOfPrimitiveOrConcept() == collection.IsEnumerableOfQueryArgumentElement(out _),
                $"Element type: {elementType}");
        }
    }
}
