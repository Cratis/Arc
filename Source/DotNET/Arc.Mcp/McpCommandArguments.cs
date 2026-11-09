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
    /// <returns>Whether the arguments satisfy the required and nullable contract.</returns>
    internal static bool Matches(JsonElement input, JsonElement schema) => Matches(input, schema, schema);

    static bool Matches(JsonElement input, JsonElement schema, JsonElement root)
    {
        if (schema.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return schema.GetBoolean();
        }

        if (schema.TryGetProperty("$ref", out var reference) && !Matches(input, Resolve(reference.GetString()!, root), root))
        {
            return false;
        }

        return MatchesValue(input, schema, root);
    }

    static bool MatchesValue(JsonElement input, JsonElement schema, JsonElement root)
    {
        if (schema.TryGetProperty("anyOf", out var alternatives) && !alternatives.EnumerateArray().Any(_ => Matches(input, _, root)))
        {
            return false;
        }
        if (input.ValueKind == JsonValueKind.Null)
        {
            return AllowsNull(schema);
        }
        if (input.ValueKind == JsonValueKind.Array && schema.TryGetProperty("items", out var items))
        {
            return input.EnumerateArray().All(_ => Matches(_, items, root));
        }
        if (input.ValueKind != JsonValueKind.Object)
        {
            return true;
        }
        return MatchesObject(input, schema, root);
    }

    static bool MatchesObject(JsonElement input, JsonElement schema, JsonElement root)
    {
        if (schema.TryGetProperty("required", out var required) && required.EnumerateArray().Any(property => !input.TryGetProperty(property.GetString()!, out _)))
        {
            return false;
        }

        return !schema.TryGetProperty("properties", out var properties) || input.EnumerateObject().All(property =>
            !properties.TryGetProperty(property.Name, out var contract) || Matches(property.Value, contract, root));
    }

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
