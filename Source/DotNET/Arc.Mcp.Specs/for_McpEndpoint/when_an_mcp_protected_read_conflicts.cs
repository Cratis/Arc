// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_an_mcp_protected_read_conflicts : a_protected_command_host
{
    async Task Because()
    {
        await SeedStock();
        _decision.BeforeReturn = AppendCompetingStock;
        await Reserve();
    }

    [Fact] void should_fail_the_mcp_tool_call() => _response.IsToolError.ShouldBeTrue();
    [Fact] void should_report_the_arc_concurrency_violation() =>
        _decision.Result!.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_return_the_arc_conflict_message_through_mcp() =>
        Outcome().GetProperty("validationResults").EnumerateArray().Single().GetProperty("message").GetString().ShouldEqual(_decision.Result!.ValidationResults.Single().Message);
    [Fact] void should_execute_the_filter_once() => _decision.Filters.ShouldEqual(1);
    [Fact] void should_execute_the_handler_once_without_retry() => _decision.Handles.ShouldEqual(1);
    [Fact] void should_keep_the_competing_event() => _appended.OfType<StockChanged>().Select(_ => _.Available).ShouldEqual([4, 2]);
    [Fact] void should_not_commit_the_owner_event() => _appended.OfType<StockReserved>().ShouldBeEmpty();
}
