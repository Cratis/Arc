// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_an_undeclared_query_is_called : an_mcp_host
{
    McpResponse _listed;
    McpResponse _called;

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _listed = await ListTools();
        _called = await CallTool($"{typeof(NoteReadModel).FullName}.{nameof(NoteReadModel.All)}", "{}");
    }

    [Fact] void should_not_list_the_query() => _listed.ToolNames.ShouldNotContain($"{typeof(NoteReadModel).FullName}.{nameof(NoteReadModel.All)}");
    [Fact] void should_refuse_the_call() => (_called.Status, _called.IsProtocolError).ShouldEqual((HttpStatusCode.OK, true));
    [Fact] void should_not_dispatch() => _probe.Count.ShouldEqual(0);
}
