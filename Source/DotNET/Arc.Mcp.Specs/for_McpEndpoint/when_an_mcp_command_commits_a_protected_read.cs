// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_an_mcp_command_commits_a_protected_read : a_protected_command_host
{
    async Task Because()
    {
        await SeedStock();
        await Reserve();
    }

    [Fact] void should_succeed_through_the_mcp_tool() => _response.IsToolError.ShouldBeFalse();
    [Fact] void should_return_a_successful_arc_result() => Outcome().GetProperty("isSuccess").GetBoolean().ShouldBeTrue();
    [Fact] void should_execute_with_read_protection_on() => _decision.ReadProtection.ShouldEqual(ReadProtectionMode.On);
    [Fact] void should_not_need_reflection_key_compatibility() =>
        _logs.Entries.Any(entry => entry.Content.Contains("uses reflection compatibility", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_execute_the_filter_once() => _decision.Filters.ShouldEqual(1);
    [Fact] void should_execute_the_handler_once() => _decision.Handles.ShouldEqual(1);
    [Fact] void should_commit_the_event_from_the_protected_model() => _appended.OfType<StockReserved>().Single().Available.ShouldEqual(4);
}
