// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_a_curated_tool_throws : an_mcp_host
{
    McpResponse _thrown;
    McpResponse _control;

    protected override void ConfigureMcp(McpBuilder mcp) => mcp.AddToolSource<ThrowingTool>();

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope);
        _thrown = await CallTool("throwing_tool", "{}");
        _control = await CallTool("who_am_i", "{}");
    }

    [Fact] void should_report_a_tool_error() => _thrown.IsToolError.ShouldBeTrue();
    [Fact] void should_not_leak_the_failure_detail_in_the_text() => _thrown.ToolText.ShouldNotContain(ThrowingTool.Detail);
    [Fact] void should_not_leak_the_failure_detail_anywhere() => _thrown.Body.ShouldNotContain(ThrowingTool.Detail);
    [Fact] void should_not_leak_the_exception_type() => _thrown.Body.ShouldNotContain(nameof(InvalidOperationException));
    [Fact] void should_keep_serving_after_the_failure() => _control.IsToolError.ShouldBeFalse();
}
