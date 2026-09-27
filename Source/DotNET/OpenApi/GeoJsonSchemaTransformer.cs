// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Geospatial;
using Cratis.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi;

/// <summary>
/// Describes the GeoJSON shape produced by Arc's geospatial converters.
/// </summary>
public class GeoJsonSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc/>
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (schema.Type is not null || (type != typeof(Point) && type != typeof(LineString) && type != typeof(Polygon)))
        {
            return Task.CompletedTask;
        }

        var converter = context.JsonTypeInfo.Options.Converters.FirstOrDefault(candidate => candidate.CanConvert(type));
        if (converter is not (PointJsonConverter or LineStringJsonConverter or PolygonJsonConverter))
        {
            return Task.CompletedTask;
        }

        IOpenApiSchema coordinates = new OpenApiSchema { Type = JsonSchemaType.Array, Items = new OpenApiSchema { Type = JsonSchemaType.Number } };
        if (type != typeof(Point))
        {
            coordinates = new OpenApiSchema { Type = JsonSchemaType.Array, Items = coordinates };
        }
        if (type == typeof(Polygon))
        {
            coordinates = new OpenApiSchema { Type = JsonSchemaType.Array, Items = coordinates };
        }

        schema.Type = JsonSchemaType.Object;
        schema.Properties = new Dictionary<string, IOpenApiSchema>
        {
            ["type"] = new OpenApiSchema { Type = JsonSchemaType.String, Enum = [JsonValue.Create(type.Name)] },
            ["coordinates"] = coordinates
        };
        return Task.CompletedTask;
    }
}
