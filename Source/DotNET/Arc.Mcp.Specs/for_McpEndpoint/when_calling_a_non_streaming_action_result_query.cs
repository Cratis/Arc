// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_calling_a_non_streaming_action_result_query : a_streaming_query_host
{
    McpResponse _response;
    JsonElement _result;

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope);
        _response = await CallTool("snapshot", "{}");
        using var document = JsonDocument.Parse(_response.ToolText);
        _result = document.RootElement.Clone();
    }

    [Fact] void should_succeed() => _response.IsToolError.ShouldBeFalse();
    [Fact] void should_return_the_unwrapped_read_model() => _result.GetProperty("data").GetProperty("text").GetString().ShouldEqual("snapshot");
    [Fact] void should_invoke_the_performer_once() => _probe.Count.ShouldEqual(1);
}
