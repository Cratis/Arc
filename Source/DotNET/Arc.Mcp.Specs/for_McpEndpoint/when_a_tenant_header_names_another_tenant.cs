// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_a_tenant_header_names_another_tenant : an_mcp_host
{
    McpResponse _curated;
    McpResponse _command;

    async Task Because()
    {
        AuthenticateIn("tenant-a", CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _curated = await Post(
            """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"who_am_i","arguments":{}}}""",
            customize: request => request.Headers.TryAddWithoutValidation("x-cratis-tenant-id", "tenant-b"));
        _command = await Post(
            """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"write_note","arguments":{"text":"x"}}}""",
            customize: request => request.Headers.TryAddWithoutValidation("x-cratis-tenant-id", "tenant-b"));
    }

    [Fact] void should_run_a_curated_tool_in_the_verified_tenant() => _curated.ToolText.ShouldEqual("person|tenant-a|mcp");
    [Fact] void should_run_a_command_in_the_verified_tenant() => _probe.Dispatched.Single(_ => _.Operation == nameof(WriteNote)).Caller!.Tenant!.Value.ShouldEqual("tenant-a");
    [Fact] void should_not_refuse_the_command() => _command.IsToolError.ShouldBeFalse();
}
