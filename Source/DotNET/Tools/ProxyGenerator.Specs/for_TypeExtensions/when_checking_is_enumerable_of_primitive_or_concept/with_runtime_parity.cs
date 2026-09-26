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
            var supported = collection.IsEnumerableOfPrimitiveOrConcept();
            Assert.True(
                supported == collection.IsEnumerableOfQueryArgumentElement(out _),
                $"Element type: {elementType}");
            if (supported)
            {
                var input = (Nullable.GetUnderlyingType(elementType) ?? elementType) switch
                {
                    var type when type == typeof(char) => "x",
                    var type when type == typeof(bool) => "true",
                    var type when type == typeof(DateTime) || type == typeof(DateTimeOffset) => "2026-05-12T14:30:45Z",
                    var type when type == typeof(Guid) => "11111111-1111-1111-1111-111111111111",
                    var type when type == typeof(TimeSpan) || type == typeof(TimeOnly) => "14:30:45",
                    var type when type == typeof(DateOnly) => "2026-05-12",
                    var type when type == typeof(Uri) => "https://example.com/a",
                    var type when type == typeof(JsonNode) || type == typeof(JsonObject) || type == typeof(JsonDocument) => "{\"id\":1}",
                    var type when type == typeof(JsonArray) => "[1,2]",
                    var type when type == typeof(SampleConcept) || type == typeof(string) => "example",
                    var type when type == typeof(DayOfWeek) => "Monday",
                    _ => "1"
                };
                object? converted = null;
                var error = Record.Exception(() => converted = new[] { input }.ConvertTo(collection));
                Assert.True(error is null, $"Failed to convert element type {elementType}: {error}");
                Assert.NotNull(converted);
                var element = Assert.Single(((System.Collections.IEnumerable)converted).Cast<object>());
                Assert.True((Nullable.GetUnderlyingType(elementType) ?? elementType).IsInstanceOfType(element),
                    $"Failed to convert element type: {elementType}");
            }
        }
    }
}
