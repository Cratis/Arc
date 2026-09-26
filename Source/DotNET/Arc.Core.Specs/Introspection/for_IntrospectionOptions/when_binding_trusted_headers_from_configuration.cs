// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Introspection.for_IntrospectionOptions;

public class when_binding_trusted_headers_from_configuration : Specification
{
    ArcOptions _options;

    void Establish() => _options = new ArcOptions();

    void Because() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Arc:Introspection:RequireAuthentication"] = "true",
            ["Cratis:Arc:Introspection:TrustForwardedIdentityHeaders"] = "true"
        })
        .Build().GetSection("Cratis:Arc").Bind(_options);

    [Fact] void should_bind_explicit_trust() => _options.Introspection.TrustForwardedIdentityHeaders.ShouldBeTrue();
}
