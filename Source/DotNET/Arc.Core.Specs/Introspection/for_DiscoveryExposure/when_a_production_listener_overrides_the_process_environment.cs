// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Introspection.for_DiscoveryExposure;

[Collection("UsesCurrentDirectory")]
public class when_a_production_listener_overrides_the_process_environment : given.a_listener_host
{
    async Task Because()
    {
        var previous = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            await RequestDiscovery(["--environment=Production"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", previous);
        }
    }

    [Fact] void should_not_expose_any_discovery_route_anonymously() => _statuses.ShouldContainOnly(Enumerable.Repeat(HttpStatusCode.NotFound, 5).ToArray());
}
