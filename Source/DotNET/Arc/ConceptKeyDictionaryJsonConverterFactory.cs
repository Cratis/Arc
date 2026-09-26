// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Concepts;
using Cratis.Json;

namespace Cratis.Arc;

/// <summary>
/// Limits Fundamentals' complex-key dictionary converter to dictionaries keyed by concepts on plain minimal APIs.
/// </summary>
internal class ConceptKeyDictionaryJsonConverterFactory : ComplexKeyDictionaryJsonConverterFactory
{
    readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> _fallbackOptions = new();

    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) =>
        base.CanConvert(typeToConvert) && typeToConvert.GetInterfaces().Append(typeToConvert)
            .Any(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>) && candidate.GetGenericArguments()[0].IsConcept());

    /// <inheritdoc/>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var keyType = typeToConvert.GetInterfaces().Append(typeToConvert)
            .First(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            .GetGenericArguments()[0];
        if (options.GetConverter(keyType).GetType() == new ConceptAsJsonConverterFactory().CreateConverter(keyType, options).GetType())
        {
            return base.CreateConverter(typeToConvert, options);
        }

        // The application's converter owns JSON property names for this key type.
        return (JsonConverter)Activator.CreateInstance(typeof(ApplicationKeyDictionaryJsonConverter<>).MakeGenericType(typeToConvert), this)!;
    }

    JsonSerializerOptions GetFallbackOptions(JsonSerializerOptions options) => _fallbackOptions.GetValue(options, original =>
    {
        var fallback = new JsonSerializerOptions(original);
        fallback.Converters.Remove(this);
        return fallback;
    });

    class ApplicationKeyDictionaryJsonConverter<TDictionary>(ConceptKeyDictionaryJsonConverterFactory factory) : JsonConverter<TDictionary>
    {
        /// <inheritdoc/>
        public override TDictionary? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            JsonSerializer.Deserialize<TDictionary>(ref reader, factory.GetFallbackOptions(options));

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TDictionary value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value, factory.GetFallbackOptions(options));
    }
}
