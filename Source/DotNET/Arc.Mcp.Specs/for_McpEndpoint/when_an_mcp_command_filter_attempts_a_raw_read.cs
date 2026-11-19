// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_an_mcp_command_filter_attempts_a_raw_read : a_protected_command_host
{
    async Task Because()
    {
        await SeedStock();
        _decision.ReadInFilter = true;
        await Reserve();
    }

    [Fact] void should_fail_the_mcp_tool_call() => _response.IsToolError.ShouldBeTrue();
    [Fact] void should_fail_closed_with_an_arc_outcome() => Outcome().GetProperty("isSuccess").GetBoolean().ShouldBeFalse();
    [Fact] void should_refuse_the_raw_read_inside_the_real_filter() => _decision.RawReadRefused.ShouldBeTrue();
    [Fact] void should_enter_the_filter_once() => _decision.Filters.ShouldEqual(1);
    [Fact] void should_not_run_the_handler() => _decision.Handles.ShouldEqual(0);
    [Fact] void should_not_commit_an_owner_event() => _appended.OfType<StockReserved>().ShouldBeEmpty();
}
