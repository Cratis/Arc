// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_a_tool_is_not_available : an_mcp_host
{
    readonly AvailabilityGateForSpecs _gate = new();
    McpResponse _listedWhenUnavailable;
    McpResponse _calledWhenUnavailable;
    int _dispatchedWhenUnavailable;
    McpResponse _listedWhenUnknown;
    McpResponse _listedWhenDecidingFails;
    McpResponse _calledWhenDecidingFails;
    int _dispatchedWhenDecidingFails;
    McpResponse _listedWhenAvailable;
    McpResponse _calledWhenAvailable;

    protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton<IAvailabilityGate>(_gate);

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);

        _gate.Parts[typeof(WriteNote)] = AvailabilityVerdict.Unavailable;
        _listedWhenUnavailable = await ListTools();
        _calledWhenUnavailable = await CallTool("write_note", """{"text":"x"}""");
        _dispatchedWhenUnavailable = _probe.Count;

        _gate.Parts[typeof(WriteNote)] = AvailabilityVerdict.Unknown;
        _listedWhenUnknown = await ListTools();

        _gate.Parts[typeof(WriteNote)] = AvailabilityVerdict.Available;
        _gate.Fail = true;
        _listedWhenDecidingFails = await ListTools();
        _calledWhenDecidingFails = await CallTool("write_note", """{"text":"x"}""");
        _dispatchedWhenDecidingFails = _probe.Count;

        _gate.Fail = false;
        _listedWhenAvailable = await ListTools();
        _calledWhenAvailable = await CallTool("write_note", """{"text":"x"}""");
    }

    [Fact] void should_not_list_an_unavailable_tool() => _listedWhenUnavailable.ToolNames.ShouldContainOnly("who_am_i");
    [Fact] void should_not_list_a_tool_whose_availability_is_unknown() => _listedWhenUnknown.ToolNames.ShouldContainOnly("who_am_i");
    [Fact] void should_refuse_a_call_to_an_unavailable_tool() => _calledWhenUnavailable.IsToolError.ShouldBeTrue();
    [Fact] void should_tell_the_caller_it_is_not_available() => _calledWhenUnavailable.ToolText.ShouldContain(AvailabilityReason.NotAvailable);
    [Fact] void should_not_dispatch_a_call_to_an_unavailable_tool() => _dispatchedWhenUnavailable.ShouldEqual(0);
    [Fact] void should_keep_listing_a_tool_when_deciding_fails() => _listedWhenDecidingFails.ToolNames.ShouldContainOnly("who_am_i", "write_note");
    [Fact] void should_refuse_a_call_as_retryable_when_deciding_fails() => _calledWhenDecidingFails.ToolText.ShouldContain("\"isRetryable\":true");
    [Fact] void should_not_report_a_denial_when_deciding_fails() => _calledWhenDecidingFails.ToolText.ShouldNotContain(AvailabilityReason.NotAvailable);
    [Fact] void should_not_dispatch_when_deciding_fails() => _dispatchedWhenDecidingFails.ShouldEqual(0);
    [Fact] void should_list_every_tool_when_available() => _listedWhenAvailable.ToolNames.ShouldContainOnly("who_am_i", "write_note");
    [Fact] void should_call_the_tool_when_available() => (_calledWhenAvailable.IsToolError, _probe.Count).ShouldEqual((false, 1));
}
