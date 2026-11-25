// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_a_tool_is_suspended : an_mcp_host
{
    readonly AvailabilityGateForSpecs _gate = new();
    McpResponse _listedWhenSuspended;
    McpResponse _calledWhenSuspended;
    int _dispatchedWhenSuspended;
    McpResponse _listedWhenResumed;

    protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton<IAvailabilityGate>(_gate);

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);

        _gate.Parts[typeof(WriteNote)] = AvailabilityVerdict.Suspended;
        _listedWhenSuspended = await ListTools();
        _calledWhenSuspended = await CallTool("write_note", """{"text":"x"}""");
        _dispatchedWhenSuspended = _probe.Count;

        _gate.Parts[typeof(WriteNote)] = AvailabilityVerdict.Available;
        _listedWhenResumed = await ListTools();
    }

    [Fact] void should_leave_the_tool_of_a_suspended_part_out_of_the_listing() => _listedWhenSuspended.ToolNames.ShouldContainOnly("who_am_i");
    [Fact] void should_refuse_a_call_to_it() => _calledWhenSuspended.IsToolError.ShouldBeTrue();
    [Fact] void should_tell_the_caller_it_is_suspended() => _calledWhenSuspended.ToolText.ShouldContain(AvailabilityReason.Suspended);
    [Fact] void should_not_dispatch_the_call() => _dispatchedWhenSuspended.ShouldEqual(0);
    [Fact] void should_list_the_tool_again_when_resumed() => _listedWhenResumed.ToolNames.ShouldContainOnly("who_am_i", "write_note");
}
