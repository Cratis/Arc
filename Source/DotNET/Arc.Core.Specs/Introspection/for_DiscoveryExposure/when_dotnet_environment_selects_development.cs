// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Introspection.for_DiscoveryExposure;

[Collection("UsesCurrentDirectory")]
public class when_dotnet_environment_selects_development : given.a_listener_host
{
    async Task Because()
    {
        var previousDotnet = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var previousAspnet = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        try
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "development");
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
            await RequestDiscovery([]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", previousDotnet);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", previousAspnet);
        }
    }

    [Fact] void should_allow_anonymous_discovery_in_the_host_development_environment() => _statuses.ShouldContainOnly(Enumerable.Repeat(HttpStatusCode.OK, 5).ToArray());
    [Fact] void should_not_warn_that_discovery_is_unavailable() => _warnings.Any(message => message.Contains("are not mapped", StringComparison.Ordinal)).ShouldBeFalse();
}
