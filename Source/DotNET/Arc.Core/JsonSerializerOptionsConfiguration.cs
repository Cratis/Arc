// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Json;
using Cratis.Serialization;

namespace Cratis.Arc;

/// <summary>
/// Extension methods for configuring <see cref="JsonSerializerOptions"/>.
/// </summary>
public static class JsonSerializerOptionsConfiguration
{
    /// <summary>
    /// Configure the <see cref="JsonSerializerOptions"/> with Arc defaults.
    /// </summary>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to configure.</param>
    /// <param name="derivedTypes">Optional <see cref="IDerivedTypes"/> to use for derived type serialization.</param>
    /// <returns>The configured <see cref="JsonSerializerOptions"/> for continuation.</returns>
    public static JsonSerializerOptions ConfigureArcDefaults(this JsonSerializerOptions options, IDerivedTypes? derivedTypes = null)
    {
        options.PropertyNamingPolicy = AcronymFriendlyJsonCamelCaseNamingPolicy.Instance;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        options.Converters.Add(new EnumConverterFactory());
        options.Converters.Add(new EnumerableConceptAsJsonConverterFactory());
        options.Converters.Add(new ConceptAsJsonConverterFactory());
        options.Converters.Add(new DateOnlyJsonConverter());
        options.Converters.Add(new TimeOnlyJsonConverter());
        options.Converters.Add(new TypeJsonConverter());
        options.Converters.Add(new UriJsonConverter());
        options.Converters.Add(new EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverterFactory());

        if (derivedTypes is not null)
        {
            options.Converters.Add(new DerivedTypeJsonConverterFactory(derivedTypes));
        }

        return options;
    }

    /// <summary>
    /// Configure the <see cref="JsonSerializerOptions"/> for ASP.NET Core with Arc defaults.
    /// This copies settings from Arc's configured options to ASP.NET's options.
    /// </summary>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to configure.</param>
    /// <param name="derivedTypes">Optional <see cref="IDerivedTypes"/> to use for derived type serialization.</param>
    /// <returns>The configured <see cref="JsonSerializerOptions"/> for continuation.</returns>
    public static JsonSerializerOptions ConfigureAspNetArcDefaults(this JsonSerializerOptions options, IDerivedTypes? derivedTypes = null)
    {
        options.PropertyNamingPolicy = AcronymFriendlyJsonCamelCaseNamingPolicy.Instance;

        // Find and remove the JsonStringEnumConverter if it exists, we want to have integers for enums.
        // The Json converter in Fundamentals gives us integer in transport.
        var converterToRemove = options.Converters
            .OfType<JsonStringEnumConverter>()
            .FirstOrDefault();

        if (converterToRemove is not null)
        {
            options.Converters.Remove(converterToRemove);
        }

        // Add the Arc default converters
        var sourceOptions = new JsonSerializerOptions().ConfigureArcDefaults(derivedTypes);
        foreach (var converter in sourceOptions.Converters)
        {
            options.Converters.Add(converter);
        }

        return options;
    }
}
