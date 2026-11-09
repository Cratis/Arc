// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_nested_command_members_alias_each_other : an_mcp_host
{
    McpResponse _nullAlias;
    McpResponse _nonNullAlias;
    McpResponse _arrayAlias;
    McpResponse _nullMember;
    McpResponse _unknownMember;

    protected override void ConfigureArcOptions(ArcOptions options)
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    }

    protected override void ConfigureMcp(McpBuilder mcp) => mcp.Expose<WriteNestedNote>("write_nested_note", "Writes a nested note.");

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _nullAlias = await CallTool("write_nested_note", """{"note_info":{"text":"ok","Text":null}}""");
        _nonNullAlias = await CallTool("write_nested_note", """{"note_info":{"text":"ok","Text":"other"}}""");
        _arrayAlias = await CallTool("write_nested_note", """{"note_info":{"text":"ok"},"notes":[{"text":"ok","Text":null}]}""");
        _nullMember = await CallTool("write_nested_note", """{"note_info":{"Text":null}}""");
        _unknownMember = await CallTool("write_nested_note", """{"note_info":{"text":"ok","unknown":"other"}}""");
    }

    [Fact] void should_refuse_a_null_alias() => _nullAlias.IsToolError.ShouldBeTrue();
    [Fact] void should_refuse_aliases_even_when_both_are_non_null() => _nonNullAlias.IsToolError.ShouldBeTrue();
    [Fact] void should_refuse_aliases_inside_arrays() => _arrayAlias.IsToolError.ShouldBeTrue();
    [Fact] void should_refuse_a_null_case_insensitive_member() => _nullMember.IsToolError.ShouldBeTrue();
    [Fact] void should_refuse_an_unknown_nested_member() => _unknownMember.IsToolError.ShouldBeTrue();
    [Fact] void should_not_dispatch() => _probe.Count.ShouldEqual(0);
}
