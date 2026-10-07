// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Introspection.for_IntrospectionOptions;

public class when_binding_identity_discovery_from_configuration : Specification
{
    ArcOptions _options = new();

    void Because() => ArcOptionsConfiguration.Bind(_options, new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Arc:Introspection:IdentityDiscovery"] = "false"
        })
        .Build().GetSection("Cratis:Arc"));

    [Fact] void should_disable_identity_discovery() => _options.Introspection.IdentityDiscovery.ShouldBeFalse();
    [Fact] void should_keep_catalogs_enabled() => _options.Introspection.Enabled.ShouldBeTrue();
    [Fact] void should_preserve_the_authentication_environment_default() => _options.Introspection.RequiresAuthentication(isDevelopment: false).ShouldBeTrue();
}
