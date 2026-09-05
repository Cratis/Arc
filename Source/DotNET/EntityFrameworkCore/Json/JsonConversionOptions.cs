// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Json;
using Cratis.Serialization;

namespace Cratis.Arc.EntityFrameworkCore.Json;

/// <summary>
/// Represents options for JSON conversion used by <see cref="JsonConversion"/>.
/// </summary>
/// <param name="derivedTypes">Optional <see cref="IDerivedTypes"/> for polymorphic type handling.</param>
public class JsonConversionOptions(IDerivedTypes? derivedTypes = null)
{
    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> used for JSON conversion.
    /// </summary>
    /// <remarks>
    /// Pre-populated with all Arc default converters. Additional converters can be added
    /// by calling <see cref="JsonSerializerOptions.Converters"/>.Add(...) on this instance,
    /// or by listing them in <see cref="EntityFrameworkCoreOptions.JsonConverters"/> before
    /// this options object is built.
    /// </remarks>
    public JsonSerializerOptions JsonSerializerOptions { get; } = CreateDefaultOptions(derivedTypes);

    static JsonSerializerOptions CreateDefaultOptions(IDerivedTypes? derivedTypes)
    {
        var options = new JsonSerializerOptions(JsonSerializerOptions.Default)
        {
            WriteIndented = false
        };
        options.Converters.Add(new ComplexKeyDictionaryJsonConverterFactory());
        options.Converters.Add(new EnumConverterFactory());
        options.Converters.Add(new EnumerableConceptAsJsonConverterFactory());
        options.Converters.Add(new ConceptAsJsonConverterFactory());
        options.Converters.Add(new DateOnlyJsonConverter());
        options.Converters.Add(new TimeOnlyJsonConverter());
        options.Converters.Add(new TypeJsonConverter());
        options.Converters.Add(new UriJsonConverter());
        options.Converters.Add(new PointJsonConverter());
        options.Converters.Add(new LineStringJsonConverter());
        options.Converters.Add(new PolygonJsonConverter());
        options.Converters.Add(new EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverterFactory());
        if (derivedTypes is not null)
        {
            options.Converters.Add(new DerivedTypeJsonConverterFactory(derivedTypes));
        }
        return options;
    }
}
