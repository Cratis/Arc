// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Capabilities;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_calling_an_exposed_command : an_mcp_host
{
    McpResponse _response;

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _response = await CallTool("write_note", """{"text":"hello"}""");
    }

    [Fact] void should_answer_ok() => _response.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_succeed() => _response.IsToolError.ShouldBeFalse();
    [Fact] void should_report_the_command_outcome() => _response.ToolText.ShouldContain("\"isSuccess\":true");
    [Fact] void should_dispatch_once() => _probe.Count.ShouldEqual(1);
    [Fact] void should_bind_the_arguments() => _probe.Dispatched.Single().Text.ShouldEqual("hello");
    [Fact] void should_run_as_the_verified_person() => _probe.Dispatched.Single().Caller!.Person.ShouldEqual("person");
    [Fact] void should_run_in_the_verified_tenant() => _probe.Dispatched.Single().Caller!.Tenant!.Value.ShouldEqual("tenant-a");
    [Fact] void should_run_over_mcp() => _probe.Dispatched.Single().Caller!.Protocol.ShouldEqual(CapabilityProtocol.Mcp);
}
