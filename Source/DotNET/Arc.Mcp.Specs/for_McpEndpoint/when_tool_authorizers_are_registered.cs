// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_tool_authorizers_are_registered : an_mcp_host
{
    readonly ToolGate _gate = new();
    McpResponse _listedWhenDenied;
    McpResponse _calledWhenDenied;
    int _dispatchedWhenDenied;
    McpResponse _listedWhenThrowing;
    McpResponse _calledWhenThrowing;
    int _dispatchedWhenThrowing;
    McpResponse _listedWhenAllowed;
    McpResponse _calledWhenAllowed;

    protected override void ConfigureMcp(McpBuilder mcp) => mcp.AddToolAuthorizer<DenyingAuthorizer>().AddToolAuthorizer<ThrowingAuthorizer>();

    protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(_gate);

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);

        _gate.Denied.Add("write_note");
        _listedWhenDenied = await ListTools();
        _calledWhenDenied = await CallTool("write_note", """{"text":"x"}""");
        _dispatchedWhenDenied = _probe.Count;

        _gate.Denied.Clear();
        _gate.Throw = true;
        _listedWhenThrowing = await ListTools();
        _calledWhenThrowing = await CallTool("write_note", """{"text":"x"}""");
        _dispatchedWhenThrowing = _probe.Count;

        _gate.Throw = false;
        _listedWhenAllowed = await ListTools();
        _calledWhenAllowed = await CallTool("write_note", """{"text":"x"}""");
    }

    [Fact] void should_not_list_a_denied_tool() => _listedWhenDenied.ToolNames.ShouldContainOnly("who_am_i");
    [Fact] void should_refuse_a_denied_call_as_forbidden() => _calledWhenDenied.Status.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_name_the_denied_refusal() => _calledWhenDenied.RefusalError.ShouldEqual("forbidden");
    [Fact] void should_not_challenge_for_scopes_when_denied() => _calledWhenDenied.Challenge.ShouldBeEmpty();
    [Fact] void should_not_dispatch_a_denied_call() => _dispatchedWhenDenied.ShouldEqual(0);
    [Fact] void should_list_nothing_when_an_authorizer_throws() => _listedWhenThrowing.ToolNames.ShouldBeEmpty();
    [Fact] void should_refuse_the_call_when_an_authorizer_throws() => _calledWhenThrowing.Status.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_not_dispatch_when_an_authorizer_throws() => _dispatchedWhenThrowing.ShouldEqual(0);
    [Fact] void should_list_every_tool_when_all_allow() => _listedWhenAllowed.ToolNames.ShouldContainOnly("who_am_i", "write_note");
    [Fact] void should_call_the_tool_when_all_allow() => (_calledWhenAllowed.IsToolError, _probe.Count).ShouldEqual((false, 1));
}
