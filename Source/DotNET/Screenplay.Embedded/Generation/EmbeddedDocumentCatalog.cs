// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Arc.Screenplay.Embedded.Generation;

/// <summary>
/// Represents everything an assembly embedded, as the navigation catalog states it.
/// </summary>
/// <param name="Documents">Every embedded document, parents before the documents beneath them.</param>
/// <remarks>
/// The catalog is written at build time and read at runtime, so both halves serialize it through
/// <see cref="SerializerOptions"/> rather than each deciding how a name is cased.
/// </remarks>
public record EmbeddedDocumentCatalog(IReadOnlyList<EmbeddedDocument> Documents)
{
    /// <summary>
    /// Represents an assembly that embedded nothing.
    /// </summary>
    public static readonly EmbeddedDocumentCatalog Empty = new([]);

    /// <summary>
    /// Gets the options the catalog is serialized with.
    /// </summary>
    /// <remarks>
    /// Camel cased names and camel cased enumeration values, so the JSON reads the way the viewer consuming it is
    /// written rather than the way the generator producing it is.
    /// </remarks>
    public static JsonSerializerOptions SerializerOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Serializes the catalog.
    /// </summary>
    /// <returns>The JSON text.</returns>
    public string Serialize() => JsonSerializer.Serialize(this, SerializerOptions);

    /// <summary>
    /// Deserializes a catalog.
    /// </summary>
    /// <param name="json">The JSON text to read.</param>
    /// <returns>The <see cref="EmbeddedDocumentCatalog"/>, empty when the text holds nothing.</returns>
    public static EmbeddedDocumentCatalog Deserialize(string json) =>
        string.IsNullOrWhiteSpace(json)
            ? Empty
            : JsonSerializer.Deserialize<EmbeddedDocumentCatalog>(json, SerializerOptions) ?? Empty;
}
