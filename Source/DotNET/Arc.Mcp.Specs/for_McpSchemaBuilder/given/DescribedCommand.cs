// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Cratis.Arc.Mcp.for_McpSchemaBuilder.given;

public record DescribedCommand(
    [property: Description("The display name.")] string DisplayName,
    [property: Description("The customer identifier.")] CustomerId Customer,
    [property: Description("The priority.")] Priority Priority,
    [property: Description("An optional age.")] int? Age,
    [property: Description("   ")] string Blank,
    string Undescribed);
