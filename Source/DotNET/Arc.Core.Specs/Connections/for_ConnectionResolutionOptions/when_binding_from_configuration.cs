// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Connections.for_ConnectionResolutionOptions;

public class when_binding_from_configuration : Specification
{
    ConnectionResolutionOptions _options;

    void Because() => _options = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Connection:Resolver"] = "Template",
            ["Connection:Template:Database"] = "app-{tenant}",
            ["Identity:Resolver"] = "Http",
            ["Identity:Http:Endpoint"] = "https://platform.example/connections",
            ["Identity:Http:Headers:X-Api-Key"] = "the-key",
            ["Identity:Http:Timeout"] = "00:00:30",
            ["CacheDuration"] = "00:01:00"
        })
        .Build()
        .Get<ConnectionResolutionOptions>()!;

    [Fact] void should_bind_the_connection_resolver() => _options.Connection.Resolver.ShouldEqual(ConnectionResolverType.Template);
    [Fact] void should_bind_the_template() => _options.Connection.Template.Database.ShouldEqual("app-{tenant}");
    [Fact] void should_bind_the_identity_resolver() => _options.Identity.Resolver.ShouldEqual(ConnectionResolverType.Http);
    [Fact] void should_bind_the_endpoint() => _options.Identity.Http.Endpoint.ShouldEqual(new Uri("https://platform.example/connections"));
    [Fact] void should_bind_the_headers() => _options.Identity.Http.Headers["X-Api-Key"].ShouldEqual("the-key");
    [Fact] void should_bind_the_timeout() => _options.Identity.Http.Timeout.ShouldEqual(TimeSpan.FromSeconds(30));
    [Fact] void should_bind_the_cache_duration() => _options.CacheDuration.ShouldEqual(TimeSpan.FromMinutes(1));
    [Fact] void should_be_configured() => _options.IsConfigured.ShouldBeTrue();
}
