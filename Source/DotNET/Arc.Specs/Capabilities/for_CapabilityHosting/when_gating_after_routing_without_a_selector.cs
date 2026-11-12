// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

[Collection("UsesCurrentDirectory")]
public class when_gating_after_routing_without_a_selector : given.a_selective_capability_host
{
    readonly Dictionary<string, int> _gated = new();
    int _offPort;

    protected override CapabilityEndpointSelector? Selector => null;
    protected override bool GateAfterRouting => true;

    async Task Because()
    {
        foreach (var path in new[] { "/mcp-spec", "/alias/mcp", "/legacy/ping", "/legacy/123", "/not-routed" })
        {
            _gated[path] = await Request(path);
        }
        _offPort = await Request("/legacy/ping", defaultPort: true);
    }

    [Fact] void should_refuse_every_route_on_the_capability_port() => _gated.Where(pair => pair.Value != 401).ShouldBeEmpty();
    [Fact] void should_never_reach_a_handler() => (_selectedCalls, _outsideMintedCaller).ShouldEqual((0, false));
    [Fact] void should_leave_other_ports_untouched() => _offPort.ShouldEqual(200);
}
