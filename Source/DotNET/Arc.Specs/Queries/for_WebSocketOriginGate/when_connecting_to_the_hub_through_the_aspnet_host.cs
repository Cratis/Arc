// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Queries.for_WebSocketOriginGate;

[Collection("UsesCurrentDirectory")]
public class when_connecting_to_the_hub_through_the_aspnet_host
{
    const string Trusted = "https://trusted.test";
    const string Other = "https://elsewhere.test";

    public static TheoryData<string?, string?, bool> Cases => new()
    {
        { null, Other, false },
        { null, null, true },
        { null, an_origin_probe_host.Same, true },
        { "", Other, false },
        { "", an_origin_probe_host.Same, true },
        { "", null, true },
        { Trusted, Other, false },
        { Trusted, "null", false },
        { Trusted, "", false },
        { Trusted, Trusted, true },
        { Trusted, an_origin_probe_host.Same, true },
        { Trusted, null, true },
        { "*", Other, true },
        { "*", "null", true },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task should_apply_the_configured_origins_and_only_run_the_query_when_connected(string? configured, string? origin, bool connects)
    {
        IList<string>? allowed = configured is null ? null : configured.Length == 0 ? [] : [configured];
        await using var host = await an_origin_probe_host.Start(allowed, false, an_origin_probe_host.RegisterHubQuery);

        (await host.SubscribesToHubQuery(origin)).ShouldEqual(connects);
        host.Probe.Queries.ShouldEqual(connects ? 1 : 0);
    }

    [Fact]
    public async Task should_answer_forbidden_before_the_upgrade_for_another_origin()
    {
        await using var host = await an_origin_probe_host.Start([Trusted], false, an_origin_probe_host.RegisterHubQuery);

        (await host.Upgrade("/.cratis/queries/ws", Other)).ShouldEqual(HttpStatusCode.Forbidden);
        host.Probe.Queries.ShouldEqual(0);
    }

    [Fact]
    public async Task should_answer_forbidden_for_another_origin_when_nothing_is_configured()
    {
        await using var host = await an_origin_probe_host.Start(null, false, an_origin_probe_host.RegisterHubQuery);

        (await host.Upgrade("/.cratis/queries/ws", Other)).ShouldEqual(HttpStatusCode.Forbidden);
        host.Probe.Queries.ShouldEqual(0);
    }
}
