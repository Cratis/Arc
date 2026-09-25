// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Cratis.Arc;

/// <summary>
/// Adds Arc's JSON conventions to minimal API serialization without replacing application converters.
/// </summary>
/// <param name="arcOptions">The configured Arc options.</param>
internal class ConfigureHttpJsonOptionsFromArcOptions(IOptions<ArcOptions> arcOptions) : IPostConfigureOptions<JsonOptions>
{
    /// <inheritdoc/>
    public void PostConfigure(string? name, JsonOptions options)
    {
        var serializerOptions = options.SerializerOptions;
        var sourceOptions = arcOptions.Value.JsonSerializerOptions;
        if (serializerOptions.PropertyNamingPolicy == JsonNamingPolicy.CamelCase)
        {
            serializerOptions.PropertyNamingPolicy = sourceOptions.PropertyNamingPolicy;
        }

        foreach (var converter in sourceOptions.Converters)
        {
            if (!serializerOptions.Converters.Any(existing => existing.GetType() == converter.GetType()))
            {
                serializerOptions.Converters.Add(converter);
            }
        }
    }
}
