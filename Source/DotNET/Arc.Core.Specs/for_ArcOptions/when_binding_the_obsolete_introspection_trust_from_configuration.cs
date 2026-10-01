// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.for_ArcOptions;

public class when_binding_the_obsolete_introspection_trust_from_configuration : Specification
{
    ArcOptions _options;

    void Establish() => _options = new ArcOptions();

    void Because() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Arc:Introspection:TrustForwardedIdentityHeaders"] = "true"
        })
        .Build().GetSection("Cratis:Arc").Bind(_options);

    [Fact] void should_trust_forwarded_identity_headers_for_the_whole_host() => _options.TrustForwardedIdentityHeaders.ShouldBeTrue();
}
