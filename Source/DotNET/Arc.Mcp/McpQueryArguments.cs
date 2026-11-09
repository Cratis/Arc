// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Maps tool arguments onto the parameters a query performer declares, the way REST maps a query string.
/// </summary>
internal static class McpQueryArguments
{
    /// <summary>
    /// Maps the arguments of a call.
    /// </summary>
    /// <param name="call">The call.</param>
    /// <param name="performer">The performer of the query.</param>
    /// <param name="options">The effective Arc serializer options.</param>
    /// <returns>The query arguments and the paging.</returns>
    /// <exception cref="McpArgumentsInvalid">An argument is unknown to the query, or paging is malformed.</exception>
    internal static (QueryArguments Arguments, Paging Paging) From(McpOperationCall call, IQueryPerformer performer, JsonSerializerOptions? options = null)
    {
        options ??= new JsonSerializerOptions().ConfigureArcDefaults();
        var arguments = new QueryArguments();
        int? page = null;
        int? pageSize = null;

        foreach (var (key, value) in call.Arguments)
        {
            if (performer.SupportsPaging && key == McpSchemaBuilder.PageName)
            {
                page = PagingValue(call, value, 0);
                continue;
            }
            if (performer.SupportsPaging && key == McpSchemaBuilder.PageSizeName)
            {
                pageSize = PagingValue(call, value, 1);
                continue;
            }

            var parameter = performer.Parameters.FirstOrDefault(_ => string.Equals(options.PropertyNamingPolicy?.ConvertName(_.Name) ?? _.Name, key, StringComparison.OrdinalIgnoreCase))
                ?? throw new McpArgumentsInvalid(call.Tool.Name);
            if (value.ValueKind == JsonValueKind.Null)
            {
                continue;
            }
            if (!arguments.TryAdd(parameter.Name, ArgumentValue(call, parameter, value)))
            {
                throw new McpArgumentsInvalid(call.Tool.Name);
            }
        }

        // Like REST, a page size is what turns paging on; a page alone leaves the query unpaged.
        var paging = pageSize is null ? Paging.NotPaged : new Paging(page ?? 0, pageSize.Value, true);
        return (arguments, paging);
    }

    static object ArgumentValue(McpOperationCall call, QueryParameter parameter, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                return value.GetString()!;

            case JsonValueKind.Array when parameter.Type.IsEnumerableOfQueryArgumentElement(out _):
                return value.EnumerateArray()
                    .Select(element => element.ValueKind is JsonValueKind.Array or JsonValueKind.Object
                        ? throw new McpArgumentsInvalid(call.Tool.Name)
                        : element.ValueKind == JsonValueKind.Null ? null : element.ToString())
                    .ToArray();

            case JsonValueKind.Array:
            case JsonValueKind.Object:
                // Structured values are only meaningful for a collection parameter, the same as for REST.
                throw new McpArgumentsInvalid(call.Tool.Name);

            default:
                return value.GetRawText();
        }
    }

    static int PagingValue(McpOperationCall call, JsonElement value, int minimum) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) && number >= minimum
            ? number
            : throw new McpArgumentsInvalid(call.Tool.Name);
}
