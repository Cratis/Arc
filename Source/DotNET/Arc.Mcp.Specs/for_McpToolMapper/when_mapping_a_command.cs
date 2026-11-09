// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.Mcp.for_McpToolCeiling.given;
using Cratis.Arc.Mcp.for_McpToolMapper.given;

namespace Cratis.Arc.Mcp.for_McpToolMapper;

public class when_mapping_a_command : a_mapper_context
{
    McpToolDescriptor _descriptor;
    JsonNode _schema;

    void Because()
    {
        _descriptor = MapCommand<DeclaredCommand>("declared_command");
        _schema = JsonNode.Parse(_descriptor.InputSchema!)!;
    }

    [Fact] void should_be_a_command_tool_for_the_command() => (_descriptor.Kind, _descriptor.Operation).ShouldEqual((McpToolKind.Command, typeof(DeclaredCommand)));
    [Fact] void should_be_a_write_tool() => _descriptor.Access.ShouldEqual(McpToolAccess.Write);
    [Fact] void should_require_only_the_write_scope() => _descriptor.RequiredScopes.ShouldContainOnly("app:write");
    [Fact] void should_not_be_read_only() => _descriptor.Annotations.ReadOnly.ShouldBeFalse();
    [Fact] void should_be_destructive() => _descriptor.Annotations.Destructive.ShouldBeTrue();
    [Fact] void should_not_be_idempotent() => _descriptor.Annotations.Idempotent.ShouldBeFalse();
    [Fact] void should_not_be_open_world() => _descriptor.Annotations.OpenWorld.ShouldBeFalse();
    [Fact] void should_not_advertise_an_ignored_idempotency_key() => _schema["properties"]!["idempotency_key"].ShouldBeNull();
    [Fact] void should_describe_the_command_input() => _schema["properties"]!["value"].ShouldNotBeNull();
    [Fact] void should_not_have_dispatched() => ShouldNotHaveDispatched();
}
