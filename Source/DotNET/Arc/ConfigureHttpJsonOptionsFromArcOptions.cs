// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Cratis.Arc;

/// <summary>
/// Adds Arc's JSON converters to minimal API serialization without changing application naming or converter precedence.
/// </summary>
/// <param name="arcOptions">The configured Arc options.</param>
internal class ConfigureHttpJsonOptionsFromArcOptions(IOptions<ArcOptions> arcOptions) : IPostConfigureOptions<JsonOptions>
{
    /// <inheritdoc/>
    public void PostConfigure(string? name, JsonOptions options)
    {
        var serializerOptions = options.SerializerOptions;
        foreach (var converter in arcOptions.Value.JsonSerializerOptions.Converters)
        {
            if (!serializerOptions.Converters.Any(existing => existing.GetType() == converter.GetType()))
            {
                serializerOptions.Converters.Add(converter);
            }
        }
    }
}
