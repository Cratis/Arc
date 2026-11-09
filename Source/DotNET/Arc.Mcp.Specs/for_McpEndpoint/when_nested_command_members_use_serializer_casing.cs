// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_nested_command_members_use_serializer_casing : an_mcp_host
{
    McpResponse _response;

    protected override void ConfigureArcOptions(ArcOptions options)
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    }

    protected override void ConfigureMcp(McpBuilder mcp) => mcp.Expose<WriteNestedNote>("write_nested_note", "Writes a nested note.");

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _response = await CallTool("write_nested_note", """{"NOTE_INFO":{"Text":"ok"},"NOTES":[{"TEXT":"other"}]}""");
    }

    [Fact] void should_accept_the_serializer_property_names() => _response.IsToolError.ShouldBeFalse();
    [Fact] void should_dispatch_once() => _probe.Count.ShouldEqual(1);
    [Fact] void should_bind_the_nested_member() => _probe.Dispatched.Single().Text.ShouldEqual("ok");
}
