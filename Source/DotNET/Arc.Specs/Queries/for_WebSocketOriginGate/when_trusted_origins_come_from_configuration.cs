// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Queries.for_WebSocketOriginGate;

[Collection("UsesCurrentDirectory")]
public class when_trusted_origins_come_from_configuration
{
    [Fact]
    public async Task should_reject_a_hostile_origin_without_running_the_query()
    {
        await using var host = await an_origin_probe_host.Start(null, true, configuration: new Dictionary<string, string?>
        {
            ["Cratis:Arc:Query:AllowedWebSocketOrigins:0"] = "https://trusted.test"
        });

        (await host.Upgrade("/origin-probe/stream", "https://hostile.test")).ShouldEqual(HttpStatusCode.Forbidden);
        host.Probe.Queries.ShouldEqual(0);
        (await host.Connects("/origin-probe/stream", "https://trusted.test")).ShouldBeTrue();
        host.Probe.Queries.ShouldEqual(1);
    }
}
