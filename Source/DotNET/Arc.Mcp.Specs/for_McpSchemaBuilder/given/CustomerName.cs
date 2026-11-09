// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Mcp.for_McpSchemaBuilder.given;

public record CustomerName(string Value) : ConceptAs<string>(Value);
