// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp.for_McpSchemaBuilder.given;

public record SchemaCommand(
    CustomerId Id,
    CustomerName Name,
    Priority Priority,
    Address Address,
    int? Age,
    IEnumerable<string> Tags,
    IReadOnlyList<Address> Previous);
