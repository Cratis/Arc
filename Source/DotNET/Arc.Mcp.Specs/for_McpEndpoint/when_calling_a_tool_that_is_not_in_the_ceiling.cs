// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_calling_a_tool_that_is_not_in_the_ceiling : an_mcp_host
{
    McpResponse _unknown;
    McpResponse _undeclaredCommand;
    McpResponse _undeclaredCommandByType;
    McpResponse _casingVariant;
    int _dispatchedWhenRefused;
    McpResponse _control;

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _unknown = await CallTool("no_such_tool", "{}");
        _undeclaredCommand = await CallTool("hidden_note", """{"text":"x"}""");
        _undeclaredCommandByType = await CallTool(nameof(HiddenNote), """{"text":"x"}""");
        _casingVariant = await CallTool("Write_Note", """{"text":"x"}""");
        _dispatchedWhenRefused = _probe.Count;
        _control = await CallTool("write_note", """{"text":"x"}""");
    }

    [Fact] void should_refuse_an_unknown_tool_as_a_protocol_error() => _unknown.IsProtocolError.ShouldBeTrue();
    [Fact] void should_refuse_an_undeclared_command_as_a_protocol_error() => _undeclaredCommand.IsProtocolError.ShouldBeTrue();
    [Fact] void should_refuse_an_undeclared_command_by_type_name_as_a_protocol_error() => _undeclaredCommandByType.IsProtocolError.ShouldBeTrue();
    [Fact] void should_refuse_a_casing_variant_as_a_protocol_error() => _casingVariant.IsProtocolError.ShouldBeTrue();
    [Fact] void should_not_dispatch_anything_refused() => _dispatchedWhenRefused.ShouldEqual(0);
    [Fact] void should_run_the_exposed_tool_in_the_same_setup() => (_control.IsToolError, _probe.Count).ShouldEqual((false, 1));
}
