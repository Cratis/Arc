// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpToolCatalog.given;

namespace Cratis.Arc.Mcp.for_McpToolCatalog;

public class when_an_approval_required_command_is_exposed : a_catalog_context
{
    Exception? _direct;
    Exception? _inherited;
    Exception? _control;

    void Because()
    {
        _direct = Catch.Exception(() => McpToolCatalog.Build(new McpExposure().Expose<ApprovalRequiredCommand>("approve_me", "Needs approval."), _options, _commands, _queries));
        _inherited = Catch.Exception(() => McpToolCatalog.Build(new McpExposure().Expose<DerivedFromApprovalRequiredCommand>("derived_approve_me", "Needs approval."), _options, _commands, _queries));
        _control = Catch.Exception(() => McpToolCatalog.Build(new McpExposure().Expose<DeclaredCommand>("declared_command", "No approval."), _options, _commands, _queries));
    }

    [Fact] void should_refuse_startup_for_the_marked_command() => _direct.ShouldBeOfExactType<McpApprovalRequiredCommandExposed>();
    [Fact] void should_refuse_startup_for_a_command_inheriting_the_requirement() => _inherited.ShouldBeOfExactType<McpApprovalRequiredCommandExposed>();
    [Fact] void should_accept_a_command_without_the_requirement() => _control.ShouldBeNull();
    [Fact] void should_not_have_dispatched() => ShouldNotHaveDispatched();
}
