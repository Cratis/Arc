// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCeiling.given;

namespace Cratis.Arc.Mcp.for_McpToolCeiling;

public class when_a_name_collides_with_an_attribute_declaration : a_ceiling_context
{
    void Establish()
    {
        AddHandlerFor<AttributedCommand>();
        _exposure.Expose<DeclaredCommand>("attributed_command", "Collides.");
    }

    void Because() => Build();

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<McpToolNameDuplicated>();
}
