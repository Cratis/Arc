// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Json;

namespace Cratis.Arc;

/// <summary>
/// Handles minimal API concept collections using the effective converter for each concept.
/// </summary>
internal class ConceptEnumerableJsonConverterFactory : EnumerableConceptAsJsonConverterFactory
{
    readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> _fallbackOptions = new();

    /// <inheritdoc/>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(ConceptEnumerableJsonConverter<>).MakeGenericType(typeToConvert), this)!;

    JsonSerializerOptions GetFallbackOptions(JsonSerializerOptions options) => _fallbackOptions.GetValue(options, original =>
    {
        var fallback = new JsonSerializerOptions(original);
        fallback.Converters.Remove(this);
        return fallback;
    });

    class ConceptEnumerableJsonConverter<TCollection>(ConceptEnumerableJsonConverterFactory factory) : JsonConverter<TCollection>
    {
        /// <inheritdoc/>
        public override TCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            JsonSerializer.Deserialize<TCollection>(ref reader, factory.GetFallbackOptions(options));

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value, factory.GetFallbackOptions(options));
    }
}
