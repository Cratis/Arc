// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Cratis.Arc;

/// <summary>
/// Adds only concept JSON converters to minimal APIs without changing application naming or converter precedence.
/// </summary>
/// <param name="arcOptions">The configured Arc options.</param>
internal class ConfigureHttpJsonOptionsFromArcOptions(IOptions<ArcOptions> arcOptions) : IPostConfigureOptions<JsonOptions>
{
    /// <inheritdoc/>
    public void PostConfigure(string? name, JsonOptions options)
    {
        var serializerOptions = options.SerializerOptions;

        // Preserve Arc's ordering: dictionary keys, concept collections, then individual concepts.
        if (!serializerOptions.Converters.Any(existing => existing is ComplexKeyDictionaryJsonConverterFactory))
        {
            serializerOptions.Converters.Add(new ConceptKeyDictionaryJsonConverterFactory());
        }

        foreach (var converter in arcOptions.Value.JsonSerializerOptions.Converters.Where(converter =>
            converter is EnumerableConceptAsJsonConverterFactory or ConceptAsJsonConverterFactory))
        {
            if (!serializerOptions.Converters.Any(existing => existing.GetType() == converter.GetType()))
            {
                serializerOptions.Converters.Add(converter);
            }
        }
    }
}
