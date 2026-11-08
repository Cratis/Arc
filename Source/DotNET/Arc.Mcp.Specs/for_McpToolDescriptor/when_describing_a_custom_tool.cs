// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp.for_McpToolDescriptor;

public class when_describing_a_custom_tool : Specification
{
    McpToolDescriptor _read;
    McpToolDescriptor _write;

    void Because()
    {
        _read = McpToolDescriptor.Custom("read_tool", "Reads.", ["app:read"], McpToolAccess.Read);
        _write = McpToolDescriptor.Custom("write_tool", "Writes.", ["app:write"], McpToolAccess.Write);
    }

    [Fact] void should_be_a_custom_tool() => _read.Kind.ShouldEqual(McpToolKind.Custom);
    [Fact] void should_have_no_operation() => _read.Operation.ShouldBeNull();
    [Fact] void should_use_the_read_hints_for_a_read() => _read.Annotations.ShouldEqual(McpToolAnnotations.ForRead);
    [Fact] void should_use_the_write_hints_for_a_write() => _write.Annotations.ShouldEqual(McpToolAnnotations.ForWrite);
}
