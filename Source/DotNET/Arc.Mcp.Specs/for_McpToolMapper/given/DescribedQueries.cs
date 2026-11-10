// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Cratis.Arc.Mcp.for_McpSchemaBuilder.given;

namespace Cratis.Arc.Mcp.for_McpToolMapper.given;

public class DescribedQueries
{
    public static IEnumerable<string> Search(
        [Description("The customer to find.")] CustomerId customerId,
        [Description("The requested priority.")] Priority priority,
        [Description("  ")] string blank,
        string undescribed) => [];

    public static IEnumerable<string> Ambiguous([Description("First overload.")] string name) => [];

    public static IEnumerable<string> Ambiguous([Description("Second overload.")] string name, int count) => [];
}
