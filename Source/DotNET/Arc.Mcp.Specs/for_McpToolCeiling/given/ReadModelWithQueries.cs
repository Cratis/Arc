// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp.for_McpToolCeiling.given;

public class ReadModelWithQueries
{
    public string Value { get; init; } = string.Empty;

    public static IEnumerable<ReadModelWithQueries> Everything() => [];

    [ExposeOverMcp("attributed_query", Description = "A query exposed by attribute.")]
    public static IEnumerable<ReadModelWithQueries> Exposed() => [];

    public static IEnumerable<ReadModelWithQueries> NotExposed() => [];
}
