// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.OpenApi;

namespace Cratis.Arc.Swagger;

/// <summary>
/// Helpers for working with the <see cref="IOpenApiMediaType"/> content of a response.
/// </summary>
static class MediaTypeUtilities
{
    const string JsonMediaType = "application/json";

    /// <summary>
    /// Sets the schema of the JSON content of a response, adding the JSON content when missing and preserving any other metadata on existing content.
    /// </summary>
    /// <param name="response">The response to set the JSON schema on.</param>
    /// <param name="schema">The schema to set.</param>
    internal static void SetJsonSchema(IOpenApiResponse? response, IOpenApiSchema schema)
    {
        if (response?.Content is null) return;

        if (response.Content.TryGetValue(JsonMediaType, out var mediaType))
        {
            // The schema of a media type is read-only on the interface, so materialize a concrete, local copy that
            // keeps examples, encoding and extensions. This also avoids mutating a shared referenced media type.
            var local = ToLocalMediaType(mediaType);
            local.Schema = schema;
            response.Content[JsonMediaType] = local;
        }
        else
        {
            response.Content.Add(JsonMediaType, new OpenApiMediaType { Schema = schema });
        }
    }

    static OpenApiMediaType ToLocalMediaType(IOpenApiMediaType mediaType) =>
        mediaType switch
        {
            OpenApiMediaType concrete => concrete,
            _ => new OpenApiMediaType
            {
                Schema = mediaType.Schema,
                ItemSchema = mediaType.ItemSchema,
                Example = mediaType.Example,
                Examples = mediaType.Examples,
                Encoding = mediaType.Encoding,
                ItemEncoding = mediaType.ItemEncoding,
                PrefixEncoding = mediaType.PrefixEncoding,
                Extensions = mediaType.Extensions is null ? null : new Dictionary<string, IOpenApiExtension>(mediaType.Extensions)
            }
        };
}
