// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_arguments_name_another_principal_or_tenant : an_mcp_host
{
    McpResponse _commandNamingTenant;
    McpResponse _commandNamingPrincipal;
    int _dispatchedWhenRefused;
    McpResponse _curated;
    McpResponse _control;

    async Task Because()
    {
        AuthenticateIn("tenant-a", CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _commandNamingTenant = await CallTool("write_note", """{"text":"x","tenant":"tenant-b"}""");
        _commandNamingPrincipal = await CallTool("write_note", """{"text":"x","person":"someone-else","principal":"someone-else"}""");
        _dispatchedWhenRefused = _probe.Count;
        _curated = await CallTool("who_am_i", """{"tenant":"tenant-b"}""");
        _control = await CallTool("write_note", """{"text":"x"}""");
    }

    [Fact] void should_refuse_a_command_argument_naming_a_tenant() => _commandNamingTenant.IsToolError.ShouldBeTrue();
    [Fact] void should_refuse_a_command_argument_naming_a_principal() => _commandNamingPrincipal.IsToolError.ShouldBeTrue();
    [Fact] void should_not_dispatch_anything_refused() => _dispatchedWhenRefused.ShouldEqual(0);
    [Fact] void should_run_a_curated_tool_as_the_verified_principal_and_tenant() => _curated.ToolText.ShouldEqual("person|tenant-a|mcp");
    [Fact] void should_run_the_command_in_the_verified_tenant_in_the_same_setup() => _probe.Dispatched.Last().Caller!.Tenant!.Value.ShouldEqual("tenant-a");
    [Fact] void should_run_the_command_in_the_same_setup() => _control.IsToolError.ShouldBeFalse();
}
