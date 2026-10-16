// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.for_ArcOptions;

public class when_binding_connection_resolution_from_configuration : Specification
{
    ArcOptions _options;

    void Establish() => _options = new ArcOptions();

    void Because() => ArcOptionsConfiguration.Bind(
        _options,
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cratis:Arc:ConnectionResolution:Identity:Resolver"] = "Http",
                ["Cratis:Arc:ConnectionResolution:Identity:Http:Endpoint"] = "https://platform.example/connections",
                ["Cratis:Arc:ConnectionResolution:Identity:Http:Headers:X-Api-Key"] = "key",
                ["Cratis:Arc:ConnectionResolution:CacheDuration"] = "00:01:00"
            })
            .Build().GetSection("Cratis:Arc"));

    [Fact] void should_be_configured() => _options.ConnectionResolution.IsConfigured.ShouldBeTrue();
    [Fact] void should_use_the_http_resolver_for_the_identity() => _options.ConnectionResolution.Identity.Resolver.ShouldEqual(ConnectionResolverType.Http);
    [Fact] void should_bind_the_endpoint() => _options.ConnectionResolution.Identity.Http.Endpoint.ShouldEqual(new Uri("https://platform.example/connections"));
    [Fact] void should_bind_the_headers() => _options.ConnectionResolution.Identity.Http.Headers["X-Api-Key"].ShouldEqual("key");
    [Fact] void should_bind_the_cache_duration() => _options.ConnectionResolution.CacheDuration.ShouldEqual(TimeSpan.FromMinutes(1));
    [Fact] void should_leave_the_connection_slot_as_default() => _options.ConnectionResolution.Connection.Resolver.ShouldEqual(ConnectionResolverType.Default);
}
