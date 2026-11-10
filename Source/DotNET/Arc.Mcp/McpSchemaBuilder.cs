// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Cratis.Arc.Queries;
using Cratis.Concepts;
using Cratis.Json;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Builds the JSON schema of a tool input from a CLR type or an Arc query's parameters.
/// </summary>
/// <remarks>
/// The schema follows the wire format Arc itself uses, so concept types appear as their underlying value type and
/// property names follow Arc's naming policy. The builder uses reflection-based metadata, on every target framework.
/// </remarks>
public static class McpSchemaBuilder
{
    /// <summary>
    /// The name of the optional page number input of a pageable query tool.
    /// </summary>
    public const string PageName = "page";

    /// <summary>
    /// The name of the optional page size input of a pageable query tool.
    /// </summary>
    public const string PageSizeName = "page_size";

    /// <summary>
    /// Builds the input schema of a command.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <returns>The JSON schema as a string.</returns>
    public static string ForCommand(Type commandType) => ForCommand(commandType, new JsonSerializerOptions().ConfigureArcDefaults());

    /// <summary>
    /// Builds the input schema using the command binding serializer options.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="options">The effective Arc serializer options.</param>
    /// <returns>The JSON schema as a string.</returns>
    public static string ForCommand(Type commandType, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(commandType);
        var schema = Export(commandType, new JsonSerializerOptions(options));
        if (schema is not JsonObject root)
        {
            root = new JsonObject();
        }

        // A command schema that resolved to something other than an object cannot be extended, so wrap it.
        if (root["type"]?.ToString() != "object")
        {
            root = new JsonObject { ["type"] = "object", ["properties"] = new JsonObject() };
        }

        root["properties"] ??= new JsonObject();
        return root.ToJsonString();
    }

    /// <summary>
    /// Builds the input schema of a query from its parameters, with optional paging inputs.
    /// </summary>
    /// <param name="parameters">The query parameters.</param>
    /// <param name="supportsPaging">Whether the query supports paging.</param>
    /// <returns>The JSON schema as a string.</returns>
    public static string ForQuery(IEnumerable<QueryParameter> parameters, bool supportsPaging) =>
        ForQuery(parameters, supportsPaging, new JsonSerializerOptions().ConfigureArcDefaults());

    /// <summary>
    /// Builds the input schema using the query binding serializer options.
    /// </summary>
    /// <param name="parameters">The query parameters.</param>
    /// <param name="supportsPaging">Whether the query supports paging.</param>
    /// <param name="options">The effective Arc serializer options.</param>
    /// <returns>The JSON schema as a string.</returns>
    public static string ForQuery(IEnumerable<QueryParameter> parameters, bool supportsPaging, JsonSerializerOptions options) =>
        ForQuery(parameters, supportsPaging, options, new Dictionary<string, string>());

    /// <summary>
    /// Builds the input schema using the query binding serializer options and explicit argument descriptions.
    /// </summary>
    /// <param name="parameters">The query parameters.</param>
    /// <param name="supportsPaging">Whether the query supports paging.</param>
    /// <param name="options">The effective Arc serializer options.</param>
    /// <param name="descriptions">The explicit argument descriptions, keyed by parameter name.</param>
    /// <returns>The JSON schema as a string.</returns>
    internal static string ForQuery(IEnumerable<QueryParameter> parameters, bool supportsPaging, JsonSerializerOptions options, IReadOnlyDictionary<string, string> descriptions)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        options = new JsonSerializerOptions(options);
        var properties = new JsonObject();
        var required = new JsonArray();
        foreach (var parameter in parameters)
        {
            descriptions.TryGetValue(parameter.Name, out var description);
            properties[ToWireName(parameter.Name, options)] = McpArgumentDescriptions.Apply(Export(parameter.Type, options), description);
            if (parameter.IsRequired)
            {
                required.Add(ToWireName(parameter.Name, options));
            }
        }

        if (supportsPaging)
        {
            properties[PageName] = new JsonObject { ["type"] = "integer", ["minimum"] = 0 };
            properties[PageSizeName] = new JsonObject { ["type"] = "integer", ["minimum"] = 1 };
        }

        var root = new JsonObject { ["type"] = "object", ["properties"] = properties };
        if (required.Count > 0)
        {
            root["required"] = required;
        }

        root["additionalProperties"] = false;
        return root.ToJsonString();
    }

    static string ToWireName(string name, JsonSerializerOptions options) => options.PropertyNamingPolicy?.ConvertName(name) ?? name;

    static JsonNode Export(Type type, JsonSerializerOptions options) => options.GetJsonSchemaAsNode(type, new JsonSchemaExporterOptions
    {
        TreatNullObliviousAsNonNullable = true,
        TransformSchemaNode = (context, schema) => Transform(context, schema, options)
    });

    static JsonNode Transform(JsonSchemaExporterContext context, JsonNode schema, JsonSerializerOptions options) =>
        McpArgumentDescriptions.Apply(TransformType(context, schema, options), McpArgumentDescriptions.DescriptionOf(context.PropertyInfo?.AttributeProvider));

    static JsonNode TransformType(JsonSchemaExporterContext context, JsonNode schema, JsonSerializerOptions options)
    {
        var type = Nullable.GetUnderlyingType(context.TypeInfo.Type) ?? context.TypeInfo.Type;
        var isNullable = type != context.TypeInfo.Type || context.PropertyInfo?.IsGetNullable == true;

        var converter = context.PropertyInfo?.CustomConverter ?? options.Converters.FirstOrDefault(_ => _.CanConvert(type));
        if (type.IsConcept() && converter is ConceptAsJsonConverterFactory)
        {
            return WithNullability(Export(type.GetConceptValueType(), options), isNullable);
        }

        if (type.IsEnum && converter is EnumConverterFactory)
        {
            var names = new JsonArray();
            Array.ForEach(Enum.GetNames(type), _ => names.Add(_));
            return WithNullability(new JsonObject { ["type"] = "string", ["enum"] = names }, isNullable);
        }

        DropNullablePropertiesFromRequired(schema);
        return schema;
    }

    static JsonNode WithNullability(JsonNode schema, bool isNullable)
    {
        var name = TypeNameOf(schema);
        if (!isNullable || name is null)
        {
            return schema;
        }

        schema["type"] = new JsonArray(name, "null");
        (schema["enum"] as JsonArray)?.Add(null);
        return schema;
    }

    static string? TypeNameOf(JsonNode schema) =>
        schema is JsonObject root && root["type"] is JsonValue type && type.TryGetValue<string>(out var name) ? name : null;

    static bool AllowsNull(JsonNode? schema) =>
        schema is JsonObject root && root["type"] is JsonArray types && types.Any(_ => _ is JsonValue value && value.TryGetValue<string>(out var name) && name == "null");

    static void DropNullablePropertiesFromRequired(JsonNode schema)
    {
        if (schema is not JsonObject root || root["required"] is not JsonArray required || root["properties"] is not JsonObject properties)
        {
            return;
        }

        var nullable = required.Where(_ => _ is not null && AllowsNull(properties[_.GetValue<string>()])).ToList();
        nullable.ForEach(_ => required.Remove(_));
        if (required.Count == 0)
        {
            root.Remove("required");
        }
    }
}
