// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Nodes;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Mcp;

/// <summary>Reads explicit argument descriptions without inferring documentation from names.</summary>
internal static class McpArgumentDescriptions
{
    internal static IReadOnlyDictionary<string, string> ForQuery(IQueryPerformer performer)
    {
        var method = performer is IAuthorizationQueryTarget target ? target.AuthorizationMethod : UnambiguousMethod(performer);
        return (method?.GetParameters() ?? [])
            .Where(_ => _.Name is not null && !string.IsNullOrWhiteSpace(DescriptionOf(_)))
            .ToDictionary(_ => _.Name!, _ => DescriptionOf(_)!, StringComparer.Ordinal);
    }

    internal static string? DescriptionOf(ICustomAttributeProvider? provider) =>
        provider?.GetCustomAttributes(typeof(DescriptionAttribute), true).OfType<DescriptionAttribute>().FirstOrDefault()?.Description;

    internal static JsonNode Apply(JsonNode schema, string? description)
    {
        if (schema is JsonObject root)
        {
            root.Remove("description");
            if (!string.IsNullOrWhiteSpace(description))
            {
                root["description"] = description;
            }
        }
        return schema;
    }

    static MethodInfo? UnambiguousMethod(IQueryPerformer performer)
    {
        var methods = performer.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(_ => _.Name == performer.Name.Value).ToArray();
        return methods.Length == 1 ? methods[0] : null;
    }
}
