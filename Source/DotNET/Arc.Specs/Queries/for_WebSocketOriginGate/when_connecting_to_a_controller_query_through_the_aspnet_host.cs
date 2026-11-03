// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Queries.for_WebSocketOriginGate;

[Collection("UsesCurrentDirectory")]
public class when_connecting_to_a_controller_query_through_the_aspnet_host
{
    const string Path = "/origin-probe/stream";
    const string Trusted = "https://trusted.test";
    const string Other = "https://elsewhere.test";
    const string ProxiedKeys = "first-proxy-key, dGhlIHNhbXBsZSBub25jZQ==";

    [Fact]
    public async Task should_answer_forbidden_without_running_the_query_for_another_origin()
    {
        await using var host = await an_origin_probe_host.Start([Trusted], true);

        (await host.Upgrade(Path, Other)).ShouldEqual(HttpStatusCode.Forbidden);
        (await host.Connects(Path, Other)).ShouldBeFalse();
        host.Probe.Queries.ShouldEqual(0);
    }

    [Fact]
    public async Task should_answer_forbidden_for_another_origin_when_the_keys_come_through_multiple_proxies()
    {
        await using var host = await an_origin_probe_host.Start([Trusted], true);

        (await host.Upgrade(Path, Other, ProxiedKeys)).ShouldEqual(HttpStatusCode.Forbidden);
        host.Probe.Queries.ShouldEqual(0);
    }

    [Fact]
    public async Task should_upgrade_for_the_same_origin_when_the_keys_come_through_multiple_proxies()
    {
        await using var host = await an_origin_probe_host.Start([Trusted], true);

        (await host.Upgrade(Path, an_origin_probe_host.Same, ProxiedKeys)).ShouldEqual(HttpStatusCode.SwitchingProtocols);
    }

    [Fact]
    public async Task should_upgrade_for_the_same_origin()
    {
        await using var host = await an_origin_probe_host.Start([Trusted], true);

        (await host.Connects(Path, an_origin_probe_host.Same)).ShouldBeTrue();
        host.Probe.Queries.ShouldEqual(1);
    }

    [Fact]
    public async Task should_upgrade_for_a_configured_origin()
    {
        await using var host = await an_origin_probe_host.Start([Trusted], true);

        (await host.Connects(Path, Trusted)).ShouldBeTrue();
    }

    [Fact]
    public async Task should_refuse_another_origin_when_nothing_is_configured()
    {
        await using var host = await an_origin_probe_host.Start(null, true);

        (await host.Connects(Path, Other)).ShouldBeFalse();
        host.Probe.Queries.ShouldEqual(0);
    }

    [Fact]
    public async Task should_refuse_another_origin_when_an_empty_list_is_configured()
    {
        await using var host = await an_origin_probe_host.Start([], true);

        (await host.Connects(Path, Other)).ShouldBeFalse();
        host.Probe.Queries.ShouldEqual(0);
    }
}
