// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

[Collection("UsesCurrentDirectory")]
public class when_selecting_endpoints_over_http : given.a_selective_capability_host
{
    readonly Dictionary<string, int> _hostile = new();
    int _legacy;
    int _parameterizedLegacy;
    int _offPort;
    int _outsideVerifications;

    async Task Because()
    {
        _legacy = await Request("/legacy/ping");
        _parameterizedLegacy = await Request("/legacy/123");
        _offPort = await Request("/mcp-spec", defaultPort: true);
        _outsideVerifications = _verified.ReceivedCalls().Count();
        _selectedCalls = 0;
        foreach (var path in new[] { "/mcp-spec", "/MCP-SPEC", "/mcp%2Dspec", "/%6Dcp-spec", "//mcp-spec", "/mcp-spec/", "/mcp-spec/.", "/legacy/../mcp-spec", "/legacy/%2e%2e/mcp-spec", "/legacy;/../mcp-spec", "/mcp-spec;parameter", "/mcp-spec%2f", "/mcp-spec%252f", "/mcp-spec/%2e", "/not-routed" })
        {
            _hostile[path] = await Request(path);
        }
        foreach (var method in new[] { "OPTIONS", "HEAD", "POST" })
        {
            _hostile[method] = await Request("/mcp-spec", method);
        }
        _hostile["spoofed headers"] = await Request("/mcp-spec", headers: "X-Forwarded-Host: legacy.example\r\nX-Forwarded-Prefix: /legacy\r\nX-Forwarded-For: 127.0.0.1\r\nX-Original-URL: /legacy/ping\r\n");
    }

    [Fact] void should_leave_legacy_routes_ungated() => _legacy.ShouldEqual(200);
    [Fact] void should_leave_parameterized_legacy_routes_ungated() => _parameterizedLegacy.ShouldEqual(200);
    [Fact] void should_leave_other_ports_untouched() => _offPort.ShouldEqual(200);
    [Fact] void should_not_verify_outside_requests() => _outsideVerifications.ShouldEqual(0);
    [Fact] void should_not_mint_an_outside_caller() => _outsideMintedCaller.ShouldBeFalse();
    [Fact] void should_refuse_every_hostile_request() => _hostile.Where(pair => pair.Value != 401).ShouldBeEmpty();
    [Fact] void should_never_reach_the_selected_handler() => _selectedCalls.ShouldEqual(0);
}
