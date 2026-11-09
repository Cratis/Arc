// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_arguments_are_malformed : an_mcp_host
{
    McpResponse _wrongType;
    McpResponse _notAnObject;
    McpResponse _undeclaredMember;
    int _dispatchedWhenRefused;
    McpResponse _control;

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _wrongType = await CallTool("write_note", """{"text":{"nested":true}}""");
        _notAnObject = await CallTool("write_note", "[1,2]");
        _undeclaredMember = await CallTool("write_note", """{"text":"x","extra":"y"}""");
        _dispatchedWhenRefused = _probe.Count;
        _control = await CallTool("write_note", """{"text":"x"}""");
    }

    [Fact] void should_refuse_a_value_of_the_wrong_type() => _wrongType.IsToolError.ShouldBeTrue();
    [Fact] void should_not_reveal_why_the_value_was_refused() => _wrongType.ToolText.ShouldEqual("The arguments are not valid for this tool.");
    [Fact] void should_refuse_arguments_that_are_not_an_object() => (_notAnObject.IsProtocolError || _notAnObject.IsToolError).ShouldBeTrue();
    [Fact] void should_refuse_an_undeclared_member() => _undeclaredMember.IsToolError.ShouldBeTrue();
    [Fact] void should_not_dispatch_anything_refused() => _dispatchedWhenRefused.ShouldEqual(0);
    [Fact] void should_run_well_formed_arguments_in_the_same_setup() => (_control.IsToolError, _probe.Count).ShouldEqual((false, 1));
}
