// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCeiling.given;
using Cratis.Arc.Mcp.for_McpToolMapper.given;

namespace Cratis.Arc.Mcp.for_McpToolMapper;

public class when_an_operation_declares_scopes : a_mapper_context
{
    McpToolDescriptor _scopedCommand;
    McpToolDescriptor _inheritingCommand;
    McpToolDescriptor _scopedQuery;
    McpToolDescriptor _control;

    void Establish()
    {
        AddHandlerFor<ScopedCommand>();
        AddHandlerFor<DerivedScopedCommand>();
        AddScopedPerformer();
    }

    void Because()
    {
        _scopedCommand = MapCommand<ScopedCommand>("scoped_command");
        _inheritingCommand = MapCommand<DerivedScopedCommand>("inheriting_command");
        _scopedQuery = MapQuery(ScopedQueryName, "scoped_query");
        _control = MapCommand<DeclaredCommand>("control");
    }

    static string Joined(McpToolDescriptor descriptor) => string.Join(',', descriptor.RequiredScopes);

    [Fact] void should_merge_type_and_method_scopes_after_the_write_scope() => Joined(_scopedCommand).ShouldEqual("app:write,app:audit,app:billing");
    [Fact] void should_not_repeat_a_scope_that_matches_the_write_scope() => _scopedCommand.RequiredScopes.Count(_ => _ == "app:write").ShouldEqual(1);
    [Fact] void should_keep_scopes_a_command_inherits() => Joined(_inheritingCommand).ShouldEqual("app:write,app:audit,app:billing");
    [Fact] void should_merge_read_model_and_query_scopes_after_the_read_scope() => Joined(_scopedQuery).ShouldEqual("app:read,app:export,app:reports");
    [Fact] void should_require_only_the_kind_scope_without_declarations() => Joined(_control).ShouldEqual("app:write");
    [Fact] void should_not_have_dispatched() => ShouldNotHaveDispatched();
}
