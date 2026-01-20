// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Cratis.Arc;

/// <summary>
/// Post-configures <see cref="JsonOptions"/> from <see cref="ArcOptions"/> to ensure controller actions
/// use the same JSON serialization settings as the rest of Arc.
/// </summary>
/// <param name="arcOptions">The configured <see cref="ArcOptions"/>.</param>
internal class ConfigureJsonOptionsFromArcOptions(IOptions<ArcOptions> arcOptions) : IPostConfigureOptions<JsonOptions>
{
    /// <inheritdoc/>
    public void PostConfigure(string? name, JsonOptions options)
    {
        var sourceOptions = arcOptions.Value.JsonSerializerOptions;

        options.JsonSerializerOptions.PropertyNamingPolicy = sourceOptions.PropertyNamingPolicy;

        // Remove JsonStringEnumConverter if present - we want integers for enums.
        // The Json converter in Fundamentals gives us integer in transport.
        var converterToRemove = options.JsonSerializerOptions.Converters
            .OfType<JsonStringEnumConverter>()
            .FirstOrDefault();

        if (converterToRemove is not null)
        {
            options.JsonSerializerOptions.Converters.Remove(converterToRemove);
        }

        // Copy converters from the configured ArcOptions
        foreach (var converter in sourceOptions.Converters)
        {
            options.JsonSerializerOptions.Converters.Add(converter);
        }
    }
}
