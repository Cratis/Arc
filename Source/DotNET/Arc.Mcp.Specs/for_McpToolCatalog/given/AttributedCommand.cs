// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp.for_McpToolCatalog.given;

[ExposeOverMcp("attributed_command", Description = "A command exposed by attribute.")]
public record AttributedCommand(string Value);
