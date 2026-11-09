// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Enforces the required and nullable input contract before command deserialization and dispatch.
/// </summary>
internal static class McpCommandArguments
{
    /// <summary>
    /// Checks required properties and nullability against the advertised command schema.
    /// </summary>
    /// <param name="input">The supplied arguments.</param>
    /// <param name="schema">The advertised schema.</param>
    /// <param name="options">The effective Arc serializer options.</param>
    /// <returns>Whether the arguments satisfy the required and nullable contract.</returns>
    internal static bool Matches(JsonElement input, JsonElement schema, JsonSerializerOptions options) =>
        Matches(input, schema, schema, options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

    static bool Matches(JsonElement input, JsonElement schema, JsonElement root, StringComparer names)
    {
        if (schema.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return schema.GetBoolean();
        }

        if (schema.TryGetProperty("$ref", out var reference) && !Matches(input, Resolve(reference.GetString()!, root), root, names))
        {
            return false;
        }

        return MatchesValue(input, schema, root, names);
    }

    static bool MatchesValue(JsonElement input, JsonElement schema, JsonElement root, StringComparer names)
    {
        if (OnlyAllowsNull(schema))
        {
            return input.ValueKind == JsonValueKind.Null;
        }
        if (schema.TryGetProperty("anyOf", out var alternatives) && !alternatives.EnumerateArray().Any(_ => Matches(input, _, root, names)))
        {
            return false;
        }
        if (input.ValueKind == JsonValueKind.Null)
        {
            return AllowsNull(schema);
        }
        if (input.ValueKind == JsonValueKind.Array && schema.TryGetProperty("items", out var items))
        {
            return input.EnumerateArray().All(_ => Matches(_, items, root, names));
        }
        if (input.ValueKind != JsonValueKind.Object)
        {
            return true;
        }
        return MatchesObject(input, schema, root, names);
    }

    static bool MatchesObject(JsonElement input, JsonElement schema, JsonElement root, StringComparer names)
    {
        if (!schema.TryGetProperty("properties", out var properties))
        {
            return !schema.TryGetProperty("additionalProperties", out var additional) ||
                input.EnumerateObject().All(property => Matches(property.Value, additional, root, names));
        }

        // Schema property names already include the naming policy and explicit JSON names used by the serializer.
        var contracts = properties.EnumerateObject().ToDictionary(property => property.Name, property => property.Value, names);
        var supplied = new HashSet<string>(names);
        foreach (var property in input.EnumerateObject())
        {
            if (!supplied.Add(property.Name))
            {
                return false;
            }
            if (!contracts.TryGetValue(property.Name, out var contract) || !Matches(property.Value, contract, root, names))
            {
                return false;
            }
        }

        return !schema.TryGetProperty("required", out var required) || required.EnumerateArray().All(property => supplied.Contains(property.GetString()!));
    }

    static bool OnlyAllowsNull(JsonElement schema) =>
        schema.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String && type.ValueEquals("null");

    static bool AllowsNull(JsonElement schema)
    {
        if (!schema.TryGetProperty("type", out var type))
        {
            return true;
        }
        return type.ValueKind == JsonValueKind.String ? type.GetString() == "null" : type.EnumerateArray().Any(_ => _.GetString() == "null");
    }

    static JsonElement Resolve(string reference, JsonElement root)
    {
        foreach (var segment in reference[1..].Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            root = root.GetProperty(segment.Replace("~1", "/").Replace("~0", "~"));
        }
        return root;
    }
}
