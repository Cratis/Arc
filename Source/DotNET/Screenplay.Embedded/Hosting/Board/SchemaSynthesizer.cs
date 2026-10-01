// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Turns the properties a Screenplay declaration states into the JSON Schema the board draws items from.
/// </summary>
internal static class SchemaSynthesizer
{
    /// <summary>
    /// Gets the JSON Schema for a set of declared properties.
    /// </summary>
    /// <param name="properties">The properties to synthesize a schema for.</param>
    /// <returns>The resulting schema.</returns>
    public static JsonObject ToSchema(this IEnumerable<PropertySyntax>? properties)
    {
        var schema = EmptyObjectSchema();
        var declared = properties?.Where(property => !string.IsNullOrWhiteSpace(property.Name)).ToList() ?? [];
        if (declared.Count == 0)
        {
            return schema;
        }

        var schemaProperties = new JsonObject();
        var required = new JsonArray();
        foreach (var property in declared)
        {
            schemaProperties[property.Name] = ToPropertySchema(property.Type);
            if (property.Type?.IsOptional != true)
            {
                required.Add(property.Name);
            }
        }

        schema["properties"] = schemaProperties;
        if (required.Count > 0)
        {
            schema["required"] = required;
        }

        return schema;
    }

    /// <summary>
    /// Gets a schema for an object with no stated properties.
    /// </summary>
    /// <returns>The resulting schema.</returns>
    public static JsonObject EmptyObjectSchema() => new() { ["type"] = "object" };

    /// <summary>
    /// Gets the kind of value a query parameter of a declared type takes.
    /// </summary>
    /// <param name="type">The declared type.</param>
    /// <returns>The resulting <see cref="QueryParameterType"/>.</returns>
    public static QueryParameterType ToQueryParameterType(this TypeRefSyntax? type) => Primitive(type?.Name) switch
    {
        "number" or "integer" => QueryParameterType.Number,
        "boolean" => QueryParameterType.Boolean,
        "date" => QueryParameterType.Date,
        "time" => QueryParameterType.Time,
        "uuid" => QueryParameterType.UniqueId,
        _ => QueryParameterType.Text
    };

    static JsonObject ToPropertySchema(TypeRefSyntax? type)
    {
        var value = Scalar(type);
        return type?.IsCollection == true
            ? new JsonObject { ["type"] = "array", ["items"] = value }
            : value;
    }

    static JsonObject Scalar(TypeRefSyntax? type) => Primitive(type?.Name) switch
    {
        "number" => new JsonObject { ["type"] = "number" },
        "integer" => new JsonObject { ["type"] = "integer" },
        "boolean" => new JsonObject { ["type"] = "boolean" },
        "date" => new JsonObject { ["type"] = "string", ["format"] = "date-time" },
        "time" => new JsonObject { ["type"] = "string", ["format"] = "time" },
        "uuid" => new JsonObject { ["type"] = "string", ["format"] = "uuid" },
        _ => new JsonObject { ["type"] = "string" }
    };

    /// <summary>
    /// Gets the primitive a declared type name reads as.
    /// </summary>
    /// <param name="name">The declared type name.</param>
    /// <returns>The primitive it reads as.</returns>
    /// <remarks>
    /// A name that is not a Screenplay primitive is a concept or a declared type; the board holds a schema per
    /// item rather than a schema registry, so it reads as the string it is written as rather than as nothing.
    /// </remarks>
    static string Primitive(string? name) => name?.ToLowerInvariant() switch
    {
        "int" or "integer" or "long" or "int32" or "int64" => "integer",
        "float" or "double" or "decimal" or "number" => "number",
        "bool" or "boolean" => "boolean",
        "date" or "datetime" or "timestamp" => "date",
        "time" or "timeonly" => "time",
        "guid" or "uuid" or "uniqueidentifier" => "uuid",
        _ => "string"
    };
}
