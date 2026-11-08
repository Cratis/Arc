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
    /// <returns>The query arguments and the paging.</returns>
    /// <exception cref="McpArgumentsInvalid">An argument is unknown to the query, or paging is malformed.</exception>
    internal static (QueryArguments Arguments, Paging Paging) From(McpOperationCall call, IQueryPerformer performer)
    {
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

            var parameter = performer.Parameters.FirstOrDefault(_ => string.Equals(_.Name, key, StringComparison.OrdinalIgnoreCase))
                ?? throw new McpArgumentsInvalid(call.Tool.Name);
            if (value.ValueKind == JsonValueKind.Null)
            {
                continue;
            }
            if (!arguments.TryAdd(parameter.Name, value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText()))
            {
                throw new McpArgumentsInvalid(call.Tool.Name);
            }
        }

        var paging = page is null && pageSize is null
            ? Paging.NotPaged
            : new Paging(page ?? 0, pageSize ?? int.MaxValue, true);
        return (arguments, paging);
    }

    static int PagingValue(McpOperationCall call, JsonElement value, int minimum) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) && number >= minimum
            ? number
            : throw new McpArgumentsInvalid(call.Tool.Name);
}
