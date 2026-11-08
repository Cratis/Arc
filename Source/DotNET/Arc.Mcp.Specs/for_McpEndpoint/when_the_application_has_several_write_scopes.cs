// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_the_application_has_several_write_scopes : an_mcp_host
{
    McpResponse _listedForReader;
    McpResponse _calledByReader;
    McpResponse _calledWithOnlyThePublishScope;
    McpResponse _calledWithPartialAlternative;
    McpResponse _listedForPublisher;
    McpResponse _calledByPublisher;
    McpResponse _calledWithAlternative;
    McpResponse _metadata;

    protected override McpProfile Profile => McpProfile.OAuthOnly;

    protected override void ConfigureMcp(McpBuilder mcp) => mcp.AddToolSource<PublishNote>();

    protected override void ConfigureOptions(McpOptions options) => options.WriteScopes.Add(PublishNote.PublishScope);

    async Task Because()
    {
        Authenticate(CallerCredentialKind.OAuthGrant, ReadScope, "audit:read");
        _listedForReader = await ListTools();
        _calledByReader = await CallTool("publish_note", "{}");

        Authenticate(CallerCredentialKind.OAuthGrant, PublishNote.PublishScope);
        _calledWithOnlyThePublishScope = await CallTool("publish_note", "{}");

        Authenticate(CallerCredentialKind.OAuthGrant, WriteScope);
        _calledWithPartialAlternative = await CallTool("publish_note", "{}");

        Authenticate(CallerCredentialKind.OAuthGrant, ReadScope, PublishNote.PublishScope);
        _listedForPublisher = await ListTools();
        _calledByPublisher = await CallTool("publish_note", "{}");

        Authenticate(CallerCredentialKind.OAuthGrant, "audit:read", WriteScope);
        _calledWithAlternative = await CallTool("publish_note", "{}");

        Unauthenticate();
        _metadata = await Get("/.well-known/oauth-protected-resource/mcp");
    }

    [Fact] void should_not_list_the_write_tool_to_a_read_only_caller() => _listedForReader.ToolNames.ShouldNotContain("publish_note");
    [Fact] void should_refuse_a_read_only_caller() => _calledByReader.Status.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_refuse_a_caller_holding_part_of_the_required_scopes() => _calledWithOnlyThePublishScope.Status.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_refuse_a_caller_holding_part_of_the_alternative() => _calledWithPartialAlternative.Status.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_list_the_write_tool_to_a_caller_holding_the_required_scopes() => _listedForPublisher.ToolNames.ShouldContain("publish_note");
    [Fact] void should_call_the_write_tool_with_the_required_scopes() => _calledByPublisher.ToolText.ShouldEqual("published");
    [Fact] void should_call_the_write_tool_with_the_alternative() => _calledWithAlternative.ToolText.ShouldEqual("published");
    [Fact] void should_advertise_every_write_scope() => _metadata.Message.GetProperty("scopes_supported").EnumerateArray().Select(_ => _.GetString()).ShouldContainOnly(ReadScope, WriteScope, PublishNote.PublishScope);
}
